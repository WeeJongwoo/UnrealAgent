namespace UnrealAgent.Backend.Security;

/// <summary>
/// 도구 실행 권한 판정 결과입니다.
/// </summary>
public enum ToolPermission
{
    /// <summary>실행을 허용합니다.</summary>
    Allow,
    /// <summary>실행을 거부합니다.</summary>
    Deny,
    /// <summary>사용자에게 확인을 요청합니다.</summary>
    Ask,
    /// <summary>이 도구를 항상 허용하도록 권한 엔진에 등록합니다.</summary>
    AlwaysAllow
}