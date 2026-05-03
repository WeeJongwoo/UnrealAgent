using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Chat;
using UnrealAgent.Backend.Security;

namespace UnrealAgent.Frontend.Page;

public partial class Chat: IAsyncDisposable
{
    /// <summary>에이전트 실행 서비스입니다.</summary>
    [Inject] private AgentRunner AgentRunner { get; set; } = null!;
    
    /// <summary>설정 패널 표시 여부입니다.</summary>
    private bool bShowSettings;

    /// <summary>설정 패널을 토글합니다.</summary>
    private void ToggleSettings() => bShowSettings = !bShowSettings;
    
    /// <summary>현재 대기 중인 권한 요청입니다.</summary>
    private ChatEvent.ToolPermissionRequest? PendingPermission;
    
    protected override void OnInitialized()
    {
        AgentRunner.OnChatEvent = OnChatEvent;
    }
    
    public ValueTask DisposeAsync()
    {
        if (AgentRunner.OnChatEvent == OnChatEvent)
            AgentRunner.OnChatEvent = null;

        return ValueTask.CompletedTask;
    }
    
    /// <summary>
    /// AgentRunner의 ChatEvent를 UI 스레드에서 처리합니다.
    /// Store 수정과 렌더링이 같은 스레드에서 실행되어 스레드 안전성을 보장합니다.
    /// </summary>
    private Task OnChatEvent(ChatEvent Evt) => InvokeAsync(() =>
    {
        if (Evt is ChatEvent.ToolPermissionRequest Req)
            PendingPermission = Req;
        else
            AgentRunner.Store.Process(Evt);
        
        // 변경된 상태를 Blazor에 렌더링 요청
        StateHasChanged();
    });
    
    /// <summary>권한 다이얼로그에서 사용자가 결정했을 때 호출됩니다.</summary>
    private void HandlePermissionDecision(ToolPermission Decision)
    {
        PendingPermission?.Tcs.TrySetResult(Decision);
        PendingPermission = null;
    }
}