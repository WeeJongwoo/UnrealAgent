using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;

namespace UnrealAgent.Frontend.UI.Messages;

public partial class SystemMessage
{
    /// <summary>표시할 시스템 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.System Message { get; set; } = null!;
}