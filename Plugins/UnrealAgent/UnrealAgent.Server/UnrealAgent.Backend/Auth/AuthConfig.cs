using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using UnrealAgent.Backend.Core;

namespace UnrealAgent.Backend.Auth;

/// <summary>
/// API Key 기반 인증 시스템
/// AuthConfig.json 파일에서 키를 로드하고 AnthropicClient를 생성
/// </summary>
public class AuthConfig
{
    private readonly string ConfigPath = Path.Combine(AgentPaths.UserConfigDir, "AuthConfig.json");
    
    /// <summary>JSON 직렬화 옵션입니다.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    /// <summary>저장된 API Key입니다.</summary>
    public string? ApiKey { get; private set; }
    
    /// <summary>현재 인증 정보로 구성된 Anthropic 클라이언트입니다.</summary>
    public AnthropicClient? Client { get; private set; }
    
    /// <summary>
    /// API Key가 설정되어 있는지 확인합니다.
    /// </summary>
    public bool IsApiKeyConfigured() => !string.IsNullOrWhiteSpace(ApiKey);
    
    /// <summary>
    /// API Key를 설정하고 파일에 저장합니다.
    /// </summary>
    public void SetApiKey(string Key)
    {
        ApiKey = Key;
        Save();
    }
    
    /// <summary>
    /// 현재 설정을 파일에 저장합니다. 디렉토리가 없으면 생성합니다.
    /// </summary>
    private void Save()
    {
        string Dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(Dir))
            Directory.CreateDirectory(Dir);

        JsonObject Root = new() { ["api_key"] = ApiKey };
        File.WriteAllText(ConfigPath, Root.ToJsonString(JsonOptions));
        UpdateClient();
    }
    
    /// <summary>
    /// 설정 파일을 로드합니다. 파일이 없으면 빈 설정을 유지합니다.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(ConfigPath))
            return;

        string Json = File.ReadAllText(ConfigPath);
        JsonNode? Root = JsonNode.Parse(Json);
        if (Root is null)
            return;

        ApiKey = Root["api_key"]?.GetValue<string>();
        UpdateClient();
    }
    
    /// <summary>
    /// API Key로 AnthropicClient를 생성합니다.
    /// </summary>
    private void UpdateClient()
    {
        Client = ApiKey is not null
            ? new AnthropicClient { ApiKey = ApiKey }
            : null;
    }
}