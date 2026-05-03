using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;

namespace UnrealAgent.Frontend.UI.Messages;

public partial class MessageBubble
{
    /// <summary>표시할 메시지입니다.</summary>
    [Parameter] public ChatUIMessage UIMessage { get; set; } = null!;
}