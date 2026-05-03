#pragma once

#include "CoreMinimal.h"
#include "Framework/Application/IInputProcessor.h"

/**
 * 글로벌 키보드 입력 프로세서입니다
 *
 * FSlateApplication에 등록되어 모든 키 입력을 먼저 가로챕니다.
 * 이를 통해 에디터 어디에 포커스가 있든 Alt+F2 단축키가 작동합니다.
 */
class FAgentChatInputProcessor : public IInputProcessor
{
public:
	explicit FAgentChatInputProcessor(const TSharedPtr<FUICommandList>& InCommands);

	//-----------------------------------------------------------------------------
	// IInputProcessor 오버라이드
	//-----------------------------------------------------------------------------
	virtual void Tick(const float DeltaTime, FSlateApplication& SlateApp, TSharedRef<ICursor> Cursor) override;
	virtual bool HandleKeyDownEvent(FSlateApplication& SlateApp, const FKeyEvent& KeyEvent) override;

private:
	/** 바인딩된 커맨드 리스트 */
	TWeakPtr<FUICommandList> Commands;
};
