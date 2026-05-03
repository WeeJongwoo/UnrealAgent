using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UnrealAgent.Frontend.Infrastructure;

/// <summary>
/// collocated JS 모듈(.razor.js)을 자동으로 로드/해제하는 컴포넌트 베이스 클래스입니다.
/// 상속 후 OnModuleLoaded()를 오버라이드하여 JS 함수를 호출하세요.
/// </summary>
public abstract class JsComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    
    /// <summary>로드된 JS 모듈 참조입니다. OnModuleLoaded() 이후 사용 가능합니다.</summary>
    protected IJSObjectReference Module = null!;
    
    /// <summary>빌드 시각 기반 캐시 무효화 버전입니다.</summary>
    private static readonly long CacheBuster = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Module = await Js.InvokeAsync<IJSObjectReference>("import", GetModulePath());
            await OnModuleLoaded();
        }
    }
    
    /// <summary> JS 모듈 로드 완료 후 호출됩니다.JS 함수 호출은 이 함수를 통해 진행하세요. </summary>
    protected virtual Task OnModuleLoaded() => Task.CompletedTask;
    
    /// <summary>컴포넌트 타입의 네임스페이스로부터 .razor.js 경로를 자동 생성합니다.</summary>
    private string GetModulePath()
    {
        // 예시: ChatInput 컴포넌트 기준
        
        // "UnrealAgent.Frontend.UI.Input"
        string Namespace = GetType().Namespace!;
        
        // "UnrealAgent.Frontend."
        const string prefix = "UnrealAgent.Frontend.";
        
        // "UI.Input" → "UI/Input"
        string Relative = Namespace[prefix.Length..].Replace('.', '/');
        
        // "ChatInput"
        string Name = GetType().Name;
        
        // "./UI/Input/ChatInput.razor.js?v=1711234567"
        return $"./{Relative}/{Name}.razor.js?v={CacheBuster}";
    }
    
    public async ValueTask DisposeAsync()
    {
        try
        {
            await Module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // 회선이 이미 끊긴 경우 무시합니다.
        }
    }
}
