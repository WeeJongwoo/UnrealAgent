#include "Chat/AgentChatInputProcessor.h"
#include "Framework/Commands/UICommandList.h"

FAgentChatInputProcessor::FAgentChatInputProcessor(const TSharedPtr<FUICommandList>& InCommands)
	: Commands(InCommands)
{
}

void FAgentChatInputProcessor::Tick(const float DeltaTime, FSlateApplication& SlateApp, TSharedRef<ICursor> Cursor)
{
	
}

bool FAgentChatInputProcessor::HandleKeyDownEvent(FSlateApplication& SlateApp, const FKeyEvent& KeyEvent)
{
	if (const TSharedPtr<FUICommandList> PinnedCommands = Commands.Pin())
	{
		return PinnedCommands->ProcessCommandBindings(KeyEvent);
	}

	return false;
}
