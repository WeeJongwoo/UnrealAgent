using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Anthropic.Models.Messages;
using UnrealAgent.Backend.Auth;
using ReverseMarkdown;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Tool.Attributes;

namespace UnrealAgent.Backend.Tool.Tools;

/// <summary>
/// 웹 페이지를 가져와 Markdown으로 변환한 뒤, AI 요약을 통해 사용자 프롬프트에 답변합니다.
/// </summary>
[AgentTool("web_fetch", """
                        Fetches content from a specified URL and processes it using an AI model.
                        - Takes a URL and a prompt as input.
                        - Fetches the URL content, converts HTML to markdown.
                        - Processes the content with the prompt using a small, fast model.
                        - Returns the model's response about the content.
                        - Use this tool when you need to retrieve and analyze web content.

                        Usage notes:
                          - The URL must be a fully-formed valid URL.
                          - HTTP URLs will be automatically upgraded to HTTPS.
                          - The prompt should describe what information you want to extract from the page.
                          - This tool is read-only and does not modify any files.
                          - Results may be summarized if the content is very large.
                          - Includes a self-cleaning 15-minute cache for faster responses when repeatedly accessing the same URL.
                        """)]
public class WebFetch(AuthConfig Auth, IHttpClientFactory HttpClientFactory) : AgentTool<WebFetch.Input>
{
    /// <summary>web_fetch 도구의 입력 파라미터입니다.</summary>
    public sealed record Input(
        [property: JsonPropertyName("url")]
        [property: Description("The URL to fetch content from")]
        string Url,

        [property: JsonPropertyName("prompt")]
        [property: Description("Instructions describing what information to extract or summarize from the fetched page")]
        string Prompt);
    
    /// <summary>URL 최대 길이입니다.</summary>
    private const int MaxUrlLength = 2_000;

    /// <summary>HTTP 응답 최대 크기입니다 (10MB).</summary>
    private const int MaxResponseBytes = 10 * 1024 * 1024;

    /// <summary>컨텐츠 자르기 임계값입니다 (100K자).</summary>
    private const int MaxContentChars = 100_000;
    
    /// <summary>캐시 TTL입니다 (15분).</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    
    /// <summary>캐시 최대 크기입니다 (50MB).</summary>
    private const long MaxCacheBytes = 50 * 1024 * 1024;
    
    /// <summary>Haiku 요약 최대 출력 토큰입니다.</summary>
    private const int SummaryMaxTokens = 4096;
    
    /// <summary>캐시 항목입니다.</summary>
    private sealed record CacheEntry(string Content, DateTime ExpiresAt, long SizeBytes);
    
    /// <summary>URL → (컨텐츠, 만료시각) LRU 캐시입니다.</summary>
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
    
    /// <summary>신뢰 도메인 목록입니다. 이 도메인의 컨텐츠는 비저작권 보호 프롬프트를 사용합니다.</summary>
    private static readonly HashSet<string> TrustedDomains =
    [
        "docs.unrealengine.com",
        "learn.microsoft.com",
        "developer.mozilla.org",
        "docs.github.com",
        "docs.python.org",
        "docs.docker.com",
        "docs.aws.amazon.com",
        "cloud.google.com",
        "docs.oracle.com",
        "docs.unity3d.com",
        "docs.godotengine.org",
        "kubernetes.io",
        "react.dev",
        "vuejs.org",
        "angular.dev",
        "nextjs.org",
        "nuxt.com",
        "svelte.dev",
        "tailwindcss.com",
        "typescriptlang.org",
        "rust-lang.org",
        "go.dev",
        "dotnet.microsoft.com",
        "kotlinlang.org",
        "docs.swift.org",
        "docs.flutter.dev",
        "pytorch.org",
        "numpy.org",
        "pandas.pydata.org",
        "graphql.org",
        "www.terraform.io"
    ];
    
    /// <summary>HTML → Markdown 변환기입니다.</summary>
    private static readonly Converter MarkdownConverter = new(new Config
    {
        GithubFlavored = true,
        RemoveComments = true,
        SmartHrefHandling = true
    });

    /// <summary>
    /// URL에서 컨텐츠를 가져와 AI 요약을 수행합니다.
    /// </summary>
    protected override async Task<ToolResult> ExecuteAsync(Input Args, AgentSession Session, CancellationToken Ct)
    {
        // 1. 인증 검증
        if (Auth.Client is null)
            return ToolResult.Error("Authentication is not configured.");
        
        // 2. URL 검증 및 HTTPS 승격
        string? ValidatedUrl = ValidateUrl(Args.Url, out string? UrlError);
        if (ValidatedUrl is null)
            return ToolResult.Error(UrlError!);
        
        // 3. 캐시 조회 — 히트 시 Fetch 건너뛰고 바로 요약
        if (TryGetCached(ValidatedUrl, out string? CachedContent))
            return await ApplyWithHaikuAsync(CachedContent!, Args.Prompt, ValidatedUrl, Ct);
        
        // 4. HTTP Fetch
        string Content;
        try
        {
            Content = await FetchAsync(ValidatedUrl, Ct);
        }
        catch (TaskCanceledException)
        {
            return ToolResult.Error($"Request timed out: {ValidatedUrl}");
        }
        catch (HttpRequestException Ex)
        {
            return ToolResult.Error($"HTTP request failed: {Ex.Message}");
        }
        
        // 5. 캐시 저장
        SetCache(ValidatedUrl, Content);
        
        // 6. AI 요약
        return await ApplyWithHaikuAsync(Content, Args.Prompt, ValidatedUrl, Ct);
    }

    /// <summary>
    /// URL을 검증하고 HTTPS로 승격합니다. 실패 시 null과 에러 메시지를 반환합니다.
    /// </summary>
    private static string? ValidateUrl(string RawUrl, out string? Error)
    {
        Error = null;
        
        // 빈 URL 차단
        if (string.IsNullOrWhiteSpace(RawUrl))
        {
            Error = "URL is empty.";
            return null;
        }
        
        // 길이 제한
        if (RawUrl.Length > MaxUrlLength)
        {
            Error = $"URL is too long (max {MaxUrlLength} characters).";
            return null;
        }
        
        // HTTP → HTTPS 승격
        string Url = RawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + RawUrl[7..]
            : RawUrl;
        
        // URL 형식 검증
        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed))
        {
            Error = $"Invalid URL: {RawUrl}";
            return null;
        }
        
        // HTTPS만 허용 (ftp:// 등 차단)
        if (Parsed.Scheme != "https")
        {
            Error = "Only HTTPS URLs are supported.";
            return null;
        }
        
        // 인증 정보 포함 차단 (예: https://user:pass@host)
        if (!string.IsNullOrEmpty(Parsed.UserInfo))
        {
            Error = "URLs with credentials are not supported.";
            return null;
        }
        
        return Url;
    }
    
    /// <summary>
    /// 캐시에서 컨텐츠를 조회합니다. 만료된 항목은 제거합니다.
    /// </summary>
    private static bool TryGetCached(string Url, out string? Content)
    {
        if (Cache.TryGetValue(Url, out CacheEntry? Entry) && Entry.ExpiresAt > DateTime.UtcNow)
        {
            Content = Entry.Content;
            return true;
        }

        Content = null;
        return false;
    }

    /// <summary>
    /// URL에서 컨텐츠를 가져와 Markdown으로 변환합니다.
    /// text/html이면 ReverseMarkdown으로 변환하고, 나머지는 패스스루합니다.
    /// </summary>
    private async Task<string> FetchAsync(string Url, CancellationToken Ct)
    {
        HttpClient Client = HttpClientFactory.CreateClient("WebFetch");
        
        // 스트리밍으로 크기 제한을 적용합니다.
        using HttpResponseMessage Response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, Ct);
        Response.EnsureSuccessStatusCode();
        
        string? ContentType = Response.Content.Headers.ContentType?.MediaType;
        
        // 비텍스트 컨텐츠는 거부합니다.
        bool bIsText = ContentType is null
                       || ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                       || ContentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                       || ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
        
        if (!bIsText)
            throw new HttpRequestException($"Non-text content type: {ContentType}");
        
        // 크기 제한을 적용하면서 읽습니다.
        await using Stream Stream = await Response.Content.ReadAsStreamAsync(Ct);
        using StreamReader Reader = new(Stream);
        char[] Buffer = new char[MaxResponseBytes];
        int TotalRead = 0;
        int Read;
        
        while (TotalRead < Buffer.Length &&
               (Read = await Reader.ReadAsync(Buffer.AsMemory(TotalRead, Buffer.Length - TotalRead), Ct)) > 0)
        {
            TotalRead += Read;
        }
        
        string RawContent = new(Buffer, 0, TotalRead);
        
        // text/html이면 Markdown으로 변환합니다.
        if (ContentType is not null &&
            ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertToMarkdown(RawContent);
        }
        
        return RawContent;
    }
    
    /// <summary>
    /// HTML을 Markdown으로 변환합니다.
    /// </summary>
    private static string ConvertToMarkdown(string Html)
    {
        return MarkdownConverter.Convert(Html);
    }
    
    /// <summary>
    /// 컨텐츠를 캐시에 저장하고, 만료된 항목을 정리합니다.
    /// </summary>
    private static void SetCache(string Url, string Content)
    {
        long SizeBytes = Content.Length * sizeof(char);
        CacheEntry Entry = new(Content, DateTime.UtcNow.Add(CacheTtl), SizeBytes);
        Cache[Url] = Entry;
        
        CleanExpired();
    }
    
    /// <summary>
    /// 만료된 캐시 항목을 제거하고, 총 크기가 제한을 초과하면 오래된 것부터 제거합니다.
    /// </summary>
    private static void CleanExpired()
    {
        DateTime Now = DateTime.UtcNow;
        
        // 만료 항목 제거
        foreach (KeyValuePair<string, CacheEntry> Pair in Cache)
        {
            if (Pair.Value.ExpiresAt <= Now)
                Cache.TryRemove(Pair.Key, out _);
        }
        
        // 크기 제한 초과 시 오래된 것부터 제거
        long TotalSize = 0;
        foreach (CacheEntry Entry in Cache.Values)
            TotalSize += Entry.SizeBytes;
        
        if (TotalSize <= MaxCacheBytes)
            return;
        
        List<KeyValuePair<string, CacheEntry>> Sorted = [.. Cache.OrderBy(P => P.Value.ExpiresAt)];
        foreach (KeyValuePair<string, CacheEntry> Pair in Sorted)
        {
            if (TotalSize <= MaxCacheBytes)
                break;

            if (Cache.TryRemove(Pair.Key, out CacheEntry? Removed))
                TotalSize -= Removed.SizeBytes;
        }

    }

    /// <summary>
    /// Haiku 4.5를 사용하여 컨텐츠를 요약합니다.
    /// 신뢰 도메인이고 100K자 미만이면 AI 요약을 건너뜁니다.
    /// </summary>
    private async Task<ToolResult> ApplyWithHaikuAsync(string Content, string Prompt, string Url, CancellationToken Ct)
    {
        // 1. 신뢰 도메인 여부 확인
        bool bIsTrusted = IsTrustedDomain(Url);
        
        // 2. 100K자 초과 시 자르기
        bool bTruncated = Content.Length > MaxContentChars;
        if (bTruncated)
            Content = Content[..MaxContentChars] + "\n\n[Content truncated due to length...]";
        
        // 3. 신뢰 도메인 + 100K 미만이면 원문 그대로 반환 (Haiku 호출 생략)
        if (bIsTrusted && !bTruncated)
            return ToolResult.Success($"Web page content:\n---\n{Content}\n---");
        
        // 4. 저작권 보호 프롬프트 구성 (신뢰/비신뢰에 따라 다른 지침)
        string CopyrightGuidance = bIsTrusted
            ? "Provide a concise response based on the content above. Include relevant details, code examples, and documentation excerpts as needed."
            : """
              Provide a concise response based only on the content above. In your response:
               - Enforce a strict 125-character maximum for quotes from any source document.
               - Use quotation marks for exact language from articles; any language outside of the quotation should never be word-for-word the same.
               - Never produce or reproduce exact song lyrics.
              """;
        
        // 5. 페이지 내용 + 사용자 프롬프트 + 저작권 지침을 합쳐서 Haiku에게 전달
        string UserPrompt = $"""
                             Web page content:
                             ---
                             {Content}
                             ---

                             {Prompt}

                             {CopyrightGuidance}
                             """;
        
        try
        {
            // 6. Haiku 4.5 서브 호출로 요약 생성
            Message Response = await Auth.Client!.Messages.Create(new MessageCreateParams
            {
                Model = "claude-haiku-4-5-20251001",
                MaxTokens = SummaryMaxTokens,
                System = new List<TextBlockParam>(),
                Messages =
                [
                    new MessageParam
                    {
                        Role = Role.User,
                        Content = UserPrompt
                    }
                ]
            }, Ct);

            // 7. 응답에서 텍스트 블록만 추출
            string ResultText = string.Join("", Response.Content
                .Where(B => B.TryPickText(out _))
                .Select(B => { B.TryPickText(out TextBlock? T); return T!.Text; }));

            return ToolResult.Success(ResultText);
        }
        catch (Exception Ex)
        {
            return ToolResult.Error($"AI summarization failed: {Ex.Message}");
        }
    }
    
    /// <summary>
    /// URL이 신뢰 도메인에 속하는지 확인합니다.
    /// </summary>
    private static bool IsTrustedDomain(string Url)
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed))
            return false;

        string Host = Parsed.Host;

        // 정확히 일치하거나 서브도메인 일치
        foreach (string Domain in TrustedDomains)
        {
            if (Host.Equals(Domain, StringComparison.OrdinalIgnoreCase) ||
                Host.EndsWith("." + Domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}