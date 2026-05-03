#include "Chat/AgentChatCommands.h"
#include "Styling/AppStyle.h"

#define LOCTEXT_NAMESPACE "FAgentChatCommands"

FAgentChatCommands::FAgentChatCommands()
	: TCommands<FAgentChatCommands>(
		TEXT("AgentChat"),
		NSLOCTEXT("Contexts", "AgentChat", "UnrealAgent Chat"),
		NAME_None,
		FAppStyle::GetAppStyleSetName())
{
}

void FAgentChatCommands::RegisterCommands()
{
	UI_COMMAND(ToggleChatPanel,
		"Toggle Chat Panel",
		"Toggle the UnrealAgent chat panel",
		EUserInterfaceActionType::Button,
		FInputChord(EModifierKey::Alt, EKeys::F2));

}

#undef LOCTEXT_NAMESPACE