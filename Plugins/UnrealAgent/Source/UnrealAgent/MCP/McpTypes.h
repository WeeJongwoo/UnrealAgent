#pragma once

#include "CoreMinimal.h"
#include "Dom/JsonObject.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"
#include "McpTypes.generated.h"

//-----------------------------------------------------------------------------
// FMcpResponse
//-----------------------------------------------------------------------------

/**
 * MCP 도구 실행 결과입니다
 *
 * McpBridge가 JSON-RPC 응답으로 변환합니다:
 * success: { "content": [{"type":"text","text":"Created: Cube_1"}], "isError": false }
 * error:   { "content": [{"type":"text","text":"NameError: ..."}],  "isError": true  }
 */
USTRUCT()
struct FMcpResponse
{
	GENERATED_BODY()

	/** 실행 성공 여부 */
	UPROPERTY()
	bool bSuccess = false;

	/** 성공 시 결과 문자열 */
	UPROPERTY()
	FString Result;

	/** 실패 시 에러 메시지 */
	UPROPERTY()
	FString Error;

	/** 성공 응답을 생성합니다 */
	static FMcpResponse Success(const FString& InResult)
	{
		FMcpResponse Response;
		Response.bSuccess = true;
		Response.Result = InResult;

		return Response;
	}

	/** 실패 응답을 생성합니다 */
	static FMcpResponse Failure(const FString& InError)
	{
		FMcpResponse Response;
		Response.bSuccess = false;
		Response.Error = InError;

		return Response;
	}

	/** 결과 텍스트를 반환합니다 (성공이면 Result, 실패면 Error) */
	const FString& GetText() const
	{
		return bSuccess ? Result : Error;
	}
};

//-----------------------------------------------------------------------------
// FMcpTool
//-----------------------------------------------------------------------------

/**
 * MCP 도구 베이스 구조체입니다
 *
 * 파생 구조체에서 UPROPERTY에 ToolParam 메타를 지정하면
 * McpBridge가 리플렉션으로 MCP inputSchema를 자동 생성하고,
 * tools/call 시 JSON arguments를 UPROPERTY에 자동 역직렬화합니다.
 *
 * 구조체 메타데이터:
 * - McpTool="name"          : MCP 도구 이름 (tools/call의 name과 매칭)
 *
 * 도구 설명:
 * - ToolDescription()을 오버라이드하여 MCP 도구 설명을 반환합니다
 * - tools/list 응답의 description 필드로 전달됩니다
 *
 * 파라미터 메타데이터:
 * - ToolParam               : 도구 파라미터로 등록 (프로퍼티 이름이 JSON 키)
 * - ToolParam="json_key"    : 도구 파라미터로 등록 (지정한 이름이 JSON 키)
 * - Required                : 필수 파라미터 (inputSchema의 required 배열에 추가)
 * - Description="..."       : 파라미터 설명 (inputSchema의 description에 추가)
 *
 * 지원 UPROPERTY 타입 → JSON Schema 타입:
 * - FString     → "string"
 * - int32       → "integer"
 * - int64       → "integer"
 * - float       → "number"
 * - double      → "number"
 * - bool        → "boolean"
 *
 * @code
 * USTRUCT(meta=(McpTool="execute_python"))
 * struct FExecutePythonTool : public FMcpTool
 * {
 *     GENERATED_BODY()
 *
 *     UPROPERTY(meta=(ToolParam="code", Required,
 *                     Description="Python code to execute"))
 *     FString Code;
 *
 *     virtual FString ToolDescription() const override { return TEXT("..."); }
 *     virtual FMcpResponse Execute() override;
 * };
 * @endcode
 */
USTRUCT(meta=(Hidden))
struct FMcpTool
{
	GENERATED_BODY()

	virtual ~FMcpTool() = default;

	/**
	 * 도구 인자 원본입니다
	 *
	 * McpBridge가 Execute() 호출 전에 ToolParam UPROPERTY를 자동 설정하므로
	 * 일반적으로 직접 접근할 필요가 없습니다.
	 */
	TSharedPtr<FJsonObject> Args;

	/** MCP 도구 설명을 반환합니다 (Claude에게 전달) */
	virtual FString ToolDescription() const { return TEXT(""); }

	/** 도구를 실행하고 결과를 반환합니다 */
	virtual FMcpResponse Execute()
	{
		return FMcpResponse::Failure(TEXT("Not implemented"));
	}
};