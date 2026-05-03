using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Auth;

namespace UnrealAgent.Frontend.UI.Settings;

public partial class SettingsPanel
{
    /// <summary>인증 설정입니다.</summary>
    [Inject] private AuthConfig Auth { get; set; } = null!;
    
    /// <summary>패널 표시 여부입니다.</summary>
    [Parameter] public bool bIsVisible { get; set; }

    /// <summary>패널 닫기 콜백입니다.</summary>
    [Parameter] public EventCallback OnClose { get; set; }
    
    /// <summary>API Key 입력값입니다.</summary>
    private string ApiKeyInput = "";

    /// <summary>상태 메시지입니다.</summary>
    private string StatusMessage = "";

    /// <summary>상태 메시지 CSS 클래스입니다.</summary>
    private string StatusCss = "";
    
    /// <summary>API Key를 저장합니다.</summary>
    private void SaveApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput))
        {
            StatusMessage = "API Key를 입력해주세요.";
            StatusCss = "text-[#e05e5e]";
            return;
        }

        Auth.SetApiKey(ApiKeyInput.Trim());
        ApiKeyInput = "";
        StatusMessage = "API Key가 저장되었습니다.";
        StatusCss = "text-[#4ba96c]";
    }
}