using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;

namespace UnrealAgent.Frontend.UI.Messages;

public partial class ChatMessages
{
    /// <summary>표시할 메시지 목록입니다. ChatStore안에 있는 메세지 랑 연결</summary>
    [Parameter] public List<ChatUIMessage> Messages { get; set; } = [];
    
    /// <summary>응답 수신 시작 여부입니다. true이면 shimmer를 숨깁니다.</summary>
    [Parameter] public bool bIsReceiving { get; set; }
}