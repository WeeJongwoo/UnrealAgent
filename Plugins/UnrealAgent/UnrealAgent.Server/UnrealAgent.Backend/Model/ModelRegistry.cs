using System.Reflection;
using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model;

/// <summary>
/// 어셈블리에서 [AgentModel] 어트리뷰트가 붙은 클래스를 스캔하여 모델 목록을 관리합니다.
/// </summary>
public sealed class ModelRegistry
{
    /// <summary> 전체 모델 배열 </summary>
    private readonly List<IModel> Models = [];
    
    /// <summary>현재(비레거시) 모델 목록입니다.</summary>
    public IReadOnlyList<IModel> CurrentModels => Models;
    
    /// <summary>ID로 모델을 찾습니다.</summary>
    public IModel? FindById(string Id) => Models.FirstOrDefault(M => M.Id == Id);
    
    /// <summary>
    /// 지정된 어셈블리에서 [Model] 클래스를 스캔합니다.
    /// Order 속성 기준으로 정렬합니다.
    /// </summary>
    public void DiscoverModels(params Assembly[] Assemblies)
    {
        List<(IModel Model, int Order)> Discovered = [];

        foreach (Assembly Asm in Assemblies)
        {
            foreach (Type Type in Asm.GetTypes())
            {
                AgentModelAttribute? Attr = Type.GetCustomAttribute<AgentModelAttribute>();
                if (Attr is null) 
                    continue;

                if (!typeof(IModel).IsAssignableFrom(Type)) 
                    continue;

                if (Activator.CreateInstance(Type) is IModel Model && !Attr.bIsLegacy)
                    Discovered.Add((Model, Attr.Order));
            }
        }

        Discovered.Sort((A, B) => A.Order.CompareTo(B.Order));
        Models.AddRange(Discovered.Select(E => E.Model));
    }
}