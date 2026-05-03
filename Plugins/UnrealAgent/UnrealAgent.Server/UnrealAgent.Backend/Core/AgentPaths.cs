namespace UnrealAgent.Backend.Core;

/// <summary>
/// 프로젝트 경로를 제공하는 정적 클래스입니다.
/// </summary>
public static class AgentPaths
{
    /// <summary>~User/.unrealagent 사용자 설정 디렉터리 경로입니다.</summary>
    public static readonly string UserConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".unrealagent");
    
    // ── 프로젝트 경로 ──

    /// <summary>UE 프로젝트 루트 경로입니다 (.uproject 파일이 위치한 디렉토리).</summary>
    public static string RootPath { get; } = string.Empty;

    /// <summary>.uproject 파일 경로입니다.</summary>
    public static string UProjectPath { get; } = string.Empty;

    /// <summary>프로젝트 레벨 설정 디렉토리 경로입니다 ({RootPath}/.unrealagent).</summary>
    public static string ConfigDir => Path.Combine(RootPath, ".unrealagent");
    
    // ── 초기화 ──

    static AgentPaths()
    {
        DirectoryInfo? Dir = new(AppContext.BaseDirectory);
        while (Dir is not null)
        {
            FileInfo? UProject = Dir.GetFiles("*.uproject").FirstOrDefault();
            
            if (UProject is not null)
            {
                RootPath = Dir.FullName;
                UProjectPath = UProject.FullName;
                
                return;
            }
            
            Dir = Dir.Parent;
        }
    }
}