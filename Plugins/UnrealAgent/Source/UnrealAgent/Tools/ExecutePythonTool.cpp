#include "Tools/ExecutePythonTool.h"
#include "IPythonScriptPlugin.h"
#include "PythonScriptTypes.h"
#include "Editor.h"
#include UE_INLINE_GENERATED_CPP_BY_NAME(ExecutePythonTool)

FMcpResponse FExecutePythonTool::Execute()
{
	// Code는 McpBridge가 ToolParam UPROPERTY에 자동 설정합니다
	if (Code.IsEmpty())
	{
		return FMcpResponse::Failure(TEXT("Empty 'code' field"));
	}

	IPythonScriptPlugin* Python = IPythonScriptPlugin::Get();
	if (!Python || !Python->IsPythonAvailable())
	{
		return FMcpResponse::Failure(TEXT("Python is not available"));
	}

	// GEditor 트랜잭션으로 래핑하여 실패 시 자동 롤백합니다
	const int32 TxIndex = GEditor->BeginTransaction(FText::FromString(TEXT("UnrealAgent: execute_python")));

	// ExecuteFile 모드: 코드를 임시 파일처럼 실행하여 여러 문(statement)을 지원합니다
	FPythonCommandEx Cmd;
	Cmd.Command = Code;
	Cmd.ExecutionMode = EPythonCommandExecutionMode::ExecuteFile;

	// PythonScriptPlugin을 통해 코드를 실행합니다
	if (Python->ExecPythonCommandEx(Cmd))
	{
		GEditor->EndTransaction();
		return FMcpResponse::Success(ExtractOutput(Cmd.LogOutput));
	}

	// 실행 실패 시 트랜잭션을 취소하여 에디터 상태를 복원합니다
	GEditor->CancelTransaction(TxIndex);
	
	return FMcpResponse::Failure(Cmd.CommandResult);
}

FString FExecutePythonTool::ExtractOutput(const TArray<struct FPythonLogOutputEntry>& LogOutput) const
{
	FString Result;

	for (const FPythonLogOutputEntry& Entry : LogOutput)
	{
		if (Entry.Type == EPythonLogOutputType::Info)
		{
			if (!Result.IsEmpty())
			{
				Result += TEXT("\n");
			}
			
			Result += Entry.Output;
		}
	}

	return Result;
}