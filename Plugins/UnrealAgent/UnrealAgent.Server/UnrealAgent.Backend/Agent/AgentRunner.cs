using Microsoft.Extensions.Hosting;
using UnrealAgent.Backend.Chat;
using UnrealAgent.Backend.Conversation;

namespace UnrealAgent.Backend.Agent;

/// <summary>
/// 메시지 큐 + 에이전트 호출 + ChatStore 관리를 담당하는 서비스입니다.
/// 리더와 팀원 모두 동일한 서비스를 사용합니다.
/// Store 수정은 직접 하지 않고 OnChatEvent를 통해 UI 스레드에서 실행합니다.
/// </summary>
public sealed class AgentRunner(AgentSession Session) : BackgroundService
{
    /// <summary>반응형 상태 관리자입니다.</summary>
    public ChatStore Store { get; } = new();
    
    /// <summary>사용자 입력을 순서대로 보관하는 메시지 큐입니다.</summary>
    private readonly Queue<UserInput> MessageQueue = new();
    
    /// <summary>큐에 메시지가 도착하면 BackgroundService 루프를 깨우는 시그널입니다.</summary>
    private readonly SemaphoreSlim Signal = new(0);
    
    /// <summary>ChatEvent 발생 시 UI 스레드에서 처리할 이벤트입니다.</summary>
    public Func<ChatEvent, Task>? OnChatEvent;
    
    /// <summary>
    /// 메시지를 큐에 추가하고 BackgroundService 루프를 깨웁니다.
    /// </summary>
    public async Task EnqueueMessage(UserInput Input)
    {
        // 사용자 메세지 UI를 위해 추가
        await DispatchEventAsync(new ChatEvent.User(Input.Text));
        
        MessageQueue.Enqueue(Input);
        Signal.Release();
    }
    
    protected override async Task ExecuteAsync(CancellationToken Ct)
    {
        while (!Ct.IsCancellationRequested)
        {
            // 시그널 대기 — EnqueueMessage 메시지 도착 시 해제
            await Signal.WaitAsync(Ct);
            
            // 메시지 큐를 순차 처리합니다.
            await DrainQueue();
        }
    }
    
    /// <summary>
    /// 큐에서 메시지를 하나씩 꺼내 순차적으로 처리합니다.
    /// </summary>
    private async Task DrainQueue()
    {
        while (MessageQueue.TryDequeue(out UserInput? Input))
        {
            try
            {
                await foreach (ChatEvent Evt in Session.ProcessMessage(Input))
                {
                    await DispatchEventAsync(Evt);
                }
            }
            catch (Exception e)
            {
                await DispatchEventAsync(new ChatEvent.System(e.Message));
            }
        }
    }
    
    /// <summary>
    /// ChatEvent를 UI 스레드로 디스패치합니다.
    /// </summary>
    private Task DispatchEventAsync(ChatEvent Evt)
    {
        if (OnChatEvent is { } Handler)
            return Handler(Evt);

        // 구독자가 없으면 (UI 미로드 상태) 직접 Store에 누적
        Store.Process(Evt);
        return Task.CompletedTask;
    }
}