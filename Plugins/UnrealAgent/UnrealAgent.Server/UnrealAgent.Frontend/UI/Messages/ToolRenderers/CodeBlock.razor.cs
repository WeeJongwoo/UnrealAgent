using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using UnrealAgent.Backend.Chat; 
using UnrealAgent.Frontend.Infrastructure;

namespace UnrealAgent.Frontend.UI.Messages.ToolRenderers;

/// <summary>
/// 코드 실행 도구 전용 렌더러입니다.
/// 도구 이름에 포함된 언어 키워드(python, java, cpp 등)를 감지하여
/// 적절한 syntax highlight 클래스를 적용합니다.
/// </summary>
public partial class CodeBlock : JsComponentBase
{
    /// <summary>도구 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.Tool Message { get; set; } = null!;
    
    /// <summary>code 요소 참조입니다.</summary>
    private ElementReference CodeRef;
    
    /// <summary>도구 이름 키워드 → Prism 언어 클래스 매핑입니다.</summary>
    private static readonly (string Keyword, string Language)[] LanguageMap =
    [
        ("python", "python"),
        ("java",   "java"),
        ("cpp",    "cpp"),
        ("csharp", "csharp"),
        ("js",     "javascript"),
        ("lua",    "lua"),
    ];
    
    /// <summary>도구 이름에서 Prism 언어를 감지합니다.</summary>
    private string DetectedLanguage
    {
        get
        {
            var ToolName = Message.Name.ToLowerInvariant();
            
            foreach (var (Keyword, Language) in LanguageMap)
            {
                if (ToolName.Contains(Keyword))
                    return Language;
            }
            
            return "plaintext";
        }
    }
    
    /// <summary>Prism.js에 전달할 언어 CSS 클래스입니다.</summary>
    private string LanguageClass => $"language-{DetectedLanguage}";

    //--------------------------------------------------------------------------
    // 정적 헬퍼
    //--------------------------------------------------------------------------

    /// <summary>이 도구가 CodeBlock으로 렌더링되어야 하는지 판별합니다.</summary>
    public static bool IsCodeTool(string ToolName)
    {
        var Lower = ToolName.ToLowerInvariant();
        return LanguageMap.Any(Entry => Lower.Contains(Entry.Keyword));
    }

    /// <summary>ToolBlock summary 바에 표시할 메타데이터를 반환합니다.</summary>
    public static ToolBlock.ToolMeta GetInfo(ChatUIMessage.Tool Msg)
        => new("code", Msg.Name, "font-mono", ChatUIMessage.Tool.GetInputField(Msg.Input, "purpose"));
    
    /// <summary>권한 다이얼로그에 표시할 요약입니다.</summary>
    public static string GetPermissionSummary(string InputJson)
        => ChatUIMessage.Tool.GetInputField(InputJson, "purpose");
    
    //--------------------------------------------------------------------------
    // 라이프사이클
    //--------------------------------------------------------------------------

    protected override async Task OnModuleLoaded()
    {
        if (!string.IsNullOrEmpty(Message.Input))
            await Module.InvokeVoidAsync("highlightCode", CodeRef);
    }

    //--------------------------------------------------------------------------
    // 포맷터
    //--------------------------------------------------------------------------

    /// <summary>JSON Input에서 code 필드를 추출합니다.</summary>
    private static string ExtractCode(string JsonInput)
    {
        if (string.IsNullOrEmpty(JsonInput))
            return "";

        try
        {
            JsonDocument Doc = JsonDocument.Parse(JsonInput);
            if (Doc.RootElement.TryGetProperty("code", out var CodeEl))
                return CodeEl.GetString() ?? JsonInput;
        }
        catch
        {
            // ignored
        }

        return JsonInput;
    }
    
    /// <summary>출력을 줄 단위로 분리합니다.</summary>
    private string[] OutputLines()
    {
        if (string.IsNullOrEmpty(Message.Content))
            return [];
        
        return Message.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
    
    /// <summary>출력 줄에 색상을 적용합니다.</summary>
    private static string FormatOutputLine(string Line)
    {
        if (Line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            return $"<span class=\"text-[#e05e5e]\">{System.Net.WebUtility.HtmlEncode(Line)}</span>";
        if (Line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
            return $"<span class=\"text-[#e5c07b]\">{System.Net.WebUtility.HtmlEncode(Line)}</span>";
        if (Line.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase))
            return $"<span class=\"text-[#98c379]\">{System.Net.WebUtility.HtmlEncode(Line)}</span>";
        return $"<span class=\"text-[#aaa]\">{System.Net.WebUtility.HtmlEncode(Line)}</span>";
    }

}