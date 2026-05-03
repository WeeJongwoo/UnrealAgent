#pragma once

#include "CoreMinimal.h"
#include "Framework/Commands/Commands.h"

/**
 * UnrealAgent 채팅 패널의 UI 커맨드입니다
 *
 * Alt+F2 단축키를 정의합니다.
 * 모듈 시작 시 Register(), 종료 시 Unregister()를 호출합니다.
 */
class FAgentChatCommands : public TCommands<FAgentChatCommands>
{
public:
	FAgentChatCommands();

	virtual void RegisterCommands() override;

	/** Alt+F2로 패널 드로어를 토글합니다 */
	TSharedPtr<FUICommandInfo> ToggleChatPanel;
};