using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Models.Messages;
using UnrealAgent.Backend.Core;

namespace UnrealAgent.Backend.Model;

/// <summary>
/// API 런타임 설정 싱글톤입니다.
/// 모델 변경 시 이 객체를 업데이트하면 즉시 반영됩니다.
/// 설정은 ~/.unrealagent/ModelSettings.json에 자동 저장됩니다.
/// </summary>
public sealed class ModelSettings(ModelRegistry Registry)
{
    /// <summary>설정 파일 경로입니다.</summary>
    private readonly string ConfigPath = Path.Combine(AgentPaths.UserConfigDir, "ModelSettings.json");
    
    /// <summary>JSON 직렬화 옵션입니다.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    /// <summary>현재 선택된 모델 정의입니다.</summary>
    private IModel CurrentModel = new Models.Opus46();
    
    /// <summary>확장된 사고 활성화 여부 백킹 필드입니다.</summary>
    private bool ThinkingEnabled = true;
    
    /// <summary>사고 깊이 백킹 필드입니다.</summary>
    private Effort CurrentEffort = Effort.High;
    
    /// <summary>API 모델 ID입니다.</summary>
    public string Model => CurrentModel.Id;
    
    /// <summary>UI 표시 이름입니다.</summary>
    public string DisplayName => CurrentModel.DisplayName;
    
    /// <summary>모델 설명입니다.</summary>
    public string Description => CurrentModel.Description;
    
    /// <summary>최대 출력 토큰 수입니다.</summary>
    public int MaxTokens => CurrentModel.MaxOutputTokens;

    /// <summary>컨텍스트 윈도우 크기입니다.</summary>
    public int ContextWindow => CurrentModel.ContextWindow;
    
    /// <summary>
    /// 현재 설정에 맞는 ThinkingConfigParam을 반환합니다.
    /// </summary>
    public ThinkingConfigParam GetThinking() => bThinkingEnabled ? new ThinkingConfigAdaptive() : new ThinkingConfigDisabled();

    /// <summary>
    /// 현재 설정에 맞는 Effort의 OutputConfig를 반환합니다.
    /// </summary>
    public OutputConfig GetEffort() => new() { Effort = Effort };
    
    /// <summary>
    /// 모델을 변경합니다.
    /// </summary>
    public void Select(IModel ClaudeModel)
    {
        CurrentModel = ClaudeModel;
        Save();
    }
    
    /// <summary>확장된 사고(Extended Thinking) 활성화 여부입니다.</summary>
    public bool bThinkingEnabled
    {
        get => ThinkingEnabled;
        set { ThinkingEnabled = value; Save(); }
    }
    
    /// <summary>Claude의 사고 깊이입니다. thinking과 독립적으로 동작합니다.</summary>
    public Effort Effort
    {
        get => CurrentEffort;
        set { CurrentEffort = value; Save(); }
    }
    
    /// <summary>
    /// 현재 설정을 파일에 저장합니다. 디렉토리가 없으면 생성합니다.
    /// </summary>
    private void Save()
    {
        string Dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(Dir))
            Directory.CreateDirectory(Dir);

        JsonObject Root = new()
        {
            ["model"] = Model,
            ["thinking_enabled"] = ThinkingEnabled,
            ["effort"] = CurrentEffort.ToString().ToLowerInvariant()
        };

        File.WriteAllText(ConfigPath, Root.ToJsonString(JsonOptions));
    }
    
    /// <summary>
    /// 설정 파일에서 로드합니다. ModelRegistry가 초기화된 후 호출해야 합니다.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(ConfigPath))
            return;

        string Json = File.ReadAllText(ConfigPath);
        JsonNode? Root = JsonNode.Parse(Json);
        if (Root is null)
            return;

        if (Root["model"]?.GetValue<string>() is { } ModelId && Registry.FindById(ModelId) is { } Found)
            CurrentModel = Found;

        if (Root["thinking_enabled"] is not null)
            ThinkingEnabled = Root["thinking_enabled"]!.GetValue<bool>();

        if (Root["effort"]?.GetValue<string>() is { } EffortStr && Enum.TryParse<Effort>(EffortStr, true, out Effort ParsedEffort))
            CurrentEffort = ParsedEffort;
    }
}