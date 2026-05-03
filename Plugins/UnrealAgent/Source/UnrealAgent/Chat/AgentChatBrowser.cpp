#include "Chat/AgentChatBrowser.h"
#include "SWebBrowser.h"
#include "WebBrowserModule.h"
#include "IWebBrowserSingleton.h"
#include "IWebBrowserWindow.h"
#include "Widgets/Docking/SDockTab.h"
#include "Framework/Application/SlateApplication.h"

void SAgentChatBrowser::Construct(const FArguments& InArgs, const TSharedRef<SDockTab>& InParentTab)
{
	// CEF 브라우저 윈도우를 생성합니다
	TSharedPtr<IWebBrowserWindow> BrowserWindow;
	if (IWebBrowserSingleton* BrowserSingleton = IWebBrowserModule::Get().GetSingleton())
	{
		FCreateBrowserWindowSettings WindowSettings;
		WindowSettings.BrowserFrameRate = 60;

		BrowserWindow = BrowserSingleton->CreateBrowserWindow(WindowSettings);
		BrowserWindow->SetParentDockTab(InParentTab);
	}
	
	// SWebBrowser 위젯을 생성합니다
	SAssignNew(WebBrowserWidget, SWebBrowser, BrowserWindow)
		.ShowControls(false)
		.ShowErrorMessage(true)
		.OnBeforePopup_Lambda([](FString Url, FString Frame) -> bool
			{
				// 팝업은 시스템 브라우저로 열고, CEF 내부 팝업은 차단합니다
				FPlatformProcess::LaunchURL(*Url, nullptr, nullptr);

				return true;
			})
		.OnLoadCompleted_Lambda([WeakThis = TWeakPtr<SAgentChatBrowser>(SharedThis(this))]()
			{
				// 페이지 로드 완료 후 키보드 포커스를 설정합니다
				if (const TSharedPtr<SAgentChatBrowser> This = WeakThis.Pin())
				{
					if (This->WebBrowserWidget.IsValid() && FSlateApplication::IsInitialized())
					{
						FSlateApplication::Get().SetKeyboardFocus(This->WebBrowserWidget, EFocusCause::SetDirectly);
					}
				}
			});

	// IME를 바인딩합니다 (한국어 등 비ASCII 입력 지원)
	if (FSlateApplication::IsInitialized())
	{
		if (ITextInputMethodSystem* InputMethodSystem = FSlateApplication::Get().GetTextInputMethodSystem())
		{
			WebBrowserWidget->BindInputMethodSystem(InputMethodSystem);
		}

		// Slate 종료 전에 IME를 언바인딩하여 댕글링 포인터 크래시를 방지합니다
		PreShutdownHandle = FSlateApplication::Get().OnPreShutdown().AddSP(
			this, &SAgentChatBrowser::HandleSlatePreShutdown);
	}

	ChildSlot
	[
		WebBrowserWidget.ToSharedRef()
	];

	// 에이전트 서버 URL을 로드합니다
	LoadServerUrl();
}

void SAgentChatBrowser::LoadServerUrl() const
{
	const FString Url = FString::Printf(TEXT("http://localhost:%d"), DefaultServerPort);
	WebBrowserWidget->LoadURL(Url);
}

FReply SAgentChatBrowser::OnKeyDown(const FGeometry& MyGeometry, const FKeyEvent& InKeyEvent)
{
	// 채팅 입력 중 에디터 단축키가 실행되지 않도록 키 이벤트를 소비합니다
	return FReply::Handled();
}

FReply SAgentChatBrowser::OnKeyUp(const FGeometry& MyGeometry, const FKeyEvent& InKeyEvent)
{
	return FReply::Handled();
}

bool SAgentChatBrowser::SupportsKeyboardFocus() const
{
	return true;
}

void SAgentChatBrowser::HandleSlatePreShutdown() const
{
	if (WebBrowserWidget.IsValid())
	{
		WebBrowserWidget->UnbindInputMethodSystem();
	}
}