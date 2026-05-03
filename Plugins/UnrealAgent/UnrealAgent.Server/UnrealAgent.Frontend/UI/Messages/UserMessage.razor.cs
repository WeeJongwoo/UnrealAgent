using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;

namespace UnrealAgent.Frontend.UI.Messages;

public partial class UserMessage
{
    /// <summary>표시할 유저 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.User Message { get; set; } = null!;
}