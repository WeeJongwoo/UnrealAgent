// Copyright Epic Games, Inc. All Rights Reserved.
#include "UnrealAgent.h"
#include "Chat/AgentChatCommands.h"
#include "Chat/AgentChatInputProcessor.h"
#include "Chat/AgentChatBrowser.h"
#include "Framework/Application/SlateApplication.h"
#include "Framework/Docking/TabManager.h"
#include "Widgets/Docking/SDockTab.h"
#include "Containers/Ticker.h"
#include "StatusBarSubsystem.h"
#include "Editor.h"

#define LOCTEXT_NAMESPACE "FUnrealAgentModule"

const FName FUnrealAgentModule::ChatTabName(TEXT("UnrealAgentChat"));

void FUnrealAgentModule::StartupModule()
{
	// 1. 커맨드 등록 (Alt+F2 단축키 정의)
	FAgentChatCommands::Register();
	
	// 2. 커맨드 리스트 생성 및 액션 바인딩
	CommandList = MakeShareable(new FUICommandList);
	
	CommandList->MapAction(
		FAgentChatCommands::Get().ToggleChatPanel,
		FExecuteAction::CreateRaw(this, &ThisClass::OnToggleChatPanel));
	
	// 3. 탭 스포너 등록
	FGlobalTabmanager::Get()->RegisterNomadTabSpawner(
		ChatTabName,
		FOnSpawnTab::CreateRaw(this, &ThisClass::OnSpawnChatTab))
		.SetDisplayName(FText::GetEmpty())
		.SetMenuType(ETabSpawnerMenuType::Hidden)
		.SetCanSidebarTab(false);
	
	// 4. 글로벌 입력 프로세서 등록 (포커스 위치와 무관하게 단축키 작동)
	InputProcessor = MakeShared<FAgentChatInputProcessor>(CommandList);
	if (FSlateApplication::IsInitialized())
	{
		FSlateApplication::Get().RegisterInputPreProcessor(InputProcessor);
	}
}

void FUnrealAgentModule::ShutdownModule()
{
	if (FSlateApplication::IsInitialized())
	{
		FSlateApplication::Get().UnregisterInputPreProcessor(InputProcessor);
	}
	InputProcessor.Reset();

	FGlobalTabmanager::Get()->UnregisterNomadTabSpawner(ChatTabName);

	FAgentChatCommands::Unregister();
}

//-----------------------------------------------------------------------------
// Chat UI
//-----------------------------------------------------------------------------

TSharedRef<SDockTab> FUnrealAgentModule::OnSpawnChatTab(const FSpawnTabArgs& SpawnTabArgs)
{
	TSharedRef<SDockTab> DockTab = SNew(SDockTab)
		.TabRole(ETabRole::NomadTab)
		.Label(INVTEXT(" "))
		.OnTabClosed_Lambda([this](TSharedRef<SDockTab>)
		{
			ChatBrowserWidget.Reset();
		});

	ChatBrowserWidget = SNew(SAgentChatBrowser, DockTab)
		.Clipping(EWidgetClipping::ClipToBounds);

	DockTab->SetContent(ChatBrowserWidget.ToSharedRef());

	return DockTab;
}

void FUnrealAgentModule::OnToggleChatPanel() const
{
	// 커서 위치의 윈도우에서 패널 드로어를 토글합니다
	TSharedPtr<FTabManager> TabManager;

	const TSharedPtr<SWidget> FocusedWidget = FSlateApplication::Get().GetKeyboardFocusedWidget();
	if (FocusedWidget.IsValid())
	{
		FWidgetPath WidgetPath;
		FSlateApplication::Get().FindPathToWidget(FocusedWidget.ToSharedRef(), WidgetPath);

		if (WidgetPath.IsValid())
		{
			TabManager = FGlobalTabmanager::Get()->GetSubTabManagerForWindow(WidgetPath.GetWindow());
		}
	}

	if (TabManager.IsValid())
	{
		PRAGMA_DISABLE_DEPRECATION_WARNINGS
		TabManager->TryToggleTabInPanelDrawer(ChatTabName, {});
		PRAGMA_ENABLE_DEPRECATION_WARNINGS
	}
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FUnrealAgentModule, UnrealAgent)