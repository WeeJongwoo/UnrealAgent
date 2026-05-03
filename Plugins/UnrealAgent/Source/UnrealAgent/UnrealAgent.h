// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "Modules/ModuleManager.h"

class FAgentChatInputProcessor;
class SAgentChatBrowser;

/**
 * UnrealAgent 플러그인 모듈입니다
 *
 * Unreal Editor를 Claude AI로 제어하는 에이전트 시스템의 진입점입니다.
 * MCP 서버, Python Executor, CEF Chat UI를 등록합니다.
 */
class FUnrealAgentModule : public IModuleInterface
{
public:
	using ThisClass = FUnrealAgentModule;
	
public:
	//-----------------------------------------------------------------------------
	// IModuleInterface 오버라이드
	//-----------------------------------------------------------------------------
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
	
private:
	//-----------------------------------------------------------------------------
	// Chat UI
	//-----------------------------------------------------------------------------

	/** 채팅 탭을 생성합니다 */
	TSharedRef<SDockTab> OnSpawnChatTab(const FSpawnTabArgs& SpawnTabArgs);

	/** 패널 드로어를 토글합니다 (Alt+F2) */
	void OnToggleChatPanel() const;
	
private:
	/** UI 커맨드 리스트 */
	TSharedPtr<FUICommandList> CommandList;

	/** 글로벌 입력 프로세서 */
	TSharedPtr<FAgentChatInputProcessor> InputProcessor;

	/** CEF 브라우저 위젯 */
	TSharedPtr<SAgentChatBrowser> ChatBrowserWidget;
	
	/** 채팅 탭 이름 */
	static const FName ChatTabName;
};
