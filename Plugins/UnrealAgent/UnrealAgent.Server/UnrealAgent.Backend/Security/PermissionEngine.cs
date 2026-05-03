using UnrealAgent.Backend.Core;
using UnrealAgent.Backend.Mode;

namespace UnrealAgent.Backend.Security;

/// <summary>
/// 도구 실행 권한 엔진입니다.
/// </summary>
public sealed class PermissionEngine
{
    /// <summary>항상 허용되는 도구 이름 집합입니다.</summary>
    private readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>도구를 허용 목록에 추가합니다.</summary>
    public void Allow(string ToolName) => AllowedTools.Add(ToolName);
    
    /// <summary>도구 호출의 실행 권한을 조회합니다.</summary>
    public Task<ToolPermission> GetPermissionAsync(Block.ToolUse ToolCall, AgentMode Mode)
    {
        // Edit 모드 확인
        if (Mode == AgentMode.Edit)
            return Task.FromResult(ToolPermission.Allow);
        
        // 허용 목록 확인
        if (AllowedTools.Contains(ToolCall.Name))
            return Task.FromResult(ToolPermission.Allow);

        // 사용자에게 확인을 요청
        return Task.FromResult(ToolPermission.Ask);
    }
}