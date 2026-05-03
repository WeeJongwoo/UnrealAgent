using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.Models.Messages;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Auth;
using UnrealAgent.Backend.Tool.Attributes;

namespace UnrealAgent.Backend.Tool.Tools;

/// <summary>
/// 웹 검색 도구입니다. 키워드 기반으로 여러 검색 결과(제목, URL, 게시일)를 반환합니다.
/// 특정 페이지의 본문을 읽지는 않습니다.
/// </summary>
[AgentTool("web_search", """
                         Allows the assistant to search the web and use the results to inform responses.
                         - Provides up-to-date information for current events and recent data.
                         - Use this tool for accessing information beyond your knowledge cutoff.
                         - Searches are performed automatically within a single API call.
                         - Domain filtering is supported to include or block specific websites.

                         IMPORTANT - Use the correct year in search queries:
                           - You MUST use the current year when searching for recent information,
                             documentation, or current events.
                           - Example: If the user asks for "latest React docs", search for
                             "React documentation" with the current year, NOT last year.
                         """)]
public class WebSearch(AuthConfig Auth) : AgentTool<WebSearch.Input>
{
    /// <summary>web_search 도구의 입력 파라미터입니다.</summary>
    public sealed record Input(
        [property: JsonPropertyName("query")]
        [property: Description("The search query to use")]
        string Query,

        [property: JsonPropertyName("allowed_domains")]
        [property: Description("Only include search results from these domains")]
        string[]? AllowedDomains = null,

        [property: JsonPropertyName("blocked_domains")]
        [property: Description("Never include search results from these domains")]
        string[]? BlockedDomains = null);

    /// <summary>서브 API 호출당 최대 검색 횟수입니다.</summary>
    private const int MaxUses = 30;

    /// <summary>
    /// 서브 API 호출로 웹 검색을 실행하고 결과를 반환합니다.
    /// </summary>
    protected override async Task<ToolResult> ExecuteAsync(Input Args, AgentSession Session, CancellationToken Ct)
    {
        if (Auth.Client is null)
            return ToolResult.Error("인증이 설정되지 않았습니다.");
        
        string CurrentMonth = DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        
        //Claude API
        WebSearchTool20250305 SearchTool = new()
        {
            AllowedDomains = Args.AllowedDomains?.ToList(),
            BlockedDomains = Args.BlockedDomains?.ToList(),
            MaxUses = MaxUses
        };
        
        // 서브 API 호출로 검색을 실행합니다.
        Message Response = await Auth.Client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 1024,
            System = new List<TextBlockParam>
            {
                new()
                {
                    Text =
                        $"You are an assistant for performing a web search tool use. The current month is {CurrentMonth}."
                }
            },
            Messages = new List<MessageParam>
            {
                new()
                {
                    Role = Role.User,
                    Content = $"Perform a web search for the query: {Args.Query}"
                }
            },
            // 툴 등록 툴 사용할 때 유니온에 등록해야됨
            Tools = new List<ToolUnion> { SearchTool }
        }, Ct);
        
        // 응답에서 검색 결과를 추출합니다.
        return ToolResult.Success(ExtractSearchResults(Response));
    }
    
    /// <summary>
    /// API 응답에서 검색 결과를 추출하여 JSON 배열로 반환합니다.
    /// </summary>
    private static string ExtractSearchResults(Message Response)
    {
        List<SearchResultEntry> Entries = [];

        foreach (ContentBlock Block in Response.Content)
        {
            if (Block.TryPickWebSearchToolResult(out WebSearchToolResultBlock? SearchResult))
            {
                if (SearchResult.Content.TryPickWebSearchResultBlocks(out IReadOnlyList<WebSearchResultBlock>? Results))
                {
                    foreach (WebSearchResultBlock Result in Results)
                        Entries.Add(new SearchResultEntry(Result.Title, Result.Url, Result.PageAge));
                }
                else if (SearchResult.Content.TryPickError(out WebSearchToolResultError? Error))
                {
                    Entries.Add(new SearchResultEntry($"Search error: {Error.ErrorCode.Raw()}", "", null));
                }
            }
        }
        
        return Entries.Count == 0 ? "No search results found." : JsonSerializer.Serialize(Entries);
    }
    
    /// <summary>검색 결과 항목입니다.</summary>
    private sealed record SearchResultEntry(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("page_age")] string? PageAge);
}