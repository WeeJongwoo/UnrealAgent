#pragma once

#include "CoreMinimal.h"
#include "Mcp/McpTypes.h"
#include "CreateCppClassTool.generated.h"

USTRUCT(meta=(McpTool="create_cpp_class"))
struct FCreateCppClassTool: public FMcpTool
{
	GENERATED_BODY()
	
	/** 생성할 클래스 이름 (UE 명명 규칙 접두사 포함, 예: "AMyActor", "UMyComponent") */
	UPROPERTY(meta=(ToolParam="class_name", Required,
					Description="Class name with UE prefix (e.g. 'AMyActor', 'UMyComponent', 'UMyWidget')"))
	FString ClassName;
	
	/** 부모 클래스 풀네임 (예: "AActor", "UObject", "UActorComponent", "UUserWidget") */
	UPROPERTY(meta=(ToolParam="parent_class", Required,
					Description="Parent class full name with prefix (e.g. 'AActor', 'UObject', 'UActorComponent', 'UUserWidget')"))
	FString ParentClass;

	/** 모듈 Source 디렉토리 기준 상대 경로 (예: "Gameplay", "UI/Widgets"). 비우면 모듈 루트 */
	UPROPERTY(meta=(ToolParam="path",
					Description="Path under module's Source dir (e.g. 'Gameplay', 'UI/Widgets'). Leave empty for module root."))
	FString Path;
	
	/** 클래스 본문 안에 삽입할 자유 코드 (UPROPERTY/UFUNCTION 매크로, 함수 선언, 멤버 변수 등) */
	UPROPERTY(meta=(ToolParam="header_body",
					Description="Free-form C++ to inject inside the class body (UPROPERTY/UFUNCTION macros, function declarations, members). Inserted between GENERATED_BODY() and the closing brace. Indent automatically applied per line."))
	FString HeaderBody;

	/** .cpp 파일 끝에 추가할 자유 코드 (함수 정의, namespace, static 변수 등) */
	UPROPERTY(meta=(ToolParam="cpp_body",
					Description="Free-form C++ appended to the .cpp file after the constructor (function definitions, namespaces, static variables). Inserted as-is, no indentation applied."))
	FString CppBody;
	
	/** 헤더 파일 상단에 추가할 #include 목록 (header_body가 참조하는 타입용). 줄바꿈 또는 쉼표로 구분 */
    UPROPERTY(meta=(ToolParam="header_includes",
                    Description="Extra #includes for the .h file (needed by header_body). One per line. Either bare path ('Components/StaticMeshComponent.h') or full directive ('#include \"Components/StaticMeshComponent.h\"')."))
    FString HeaderIncludes;
    
        /** .cpp 파일 상단에 추가할 #include 목록 (cpp_body가 참조하는 타입용). 줄바꿈 또는 쉼표로 구분 */
    UPROPERTY(meta=(ToolParam="cpp_includes",
                    Description="Extra #includes for the .cpp file (needed by cpp_body). One per line. Either bare path ('Kismet/GameplayStatics.h') or full directive."))
    FString CppIncludes;
	
	/** MCP 도구 설명을 반환합니다 (Claude에게 전달) */
	virtual FString ToolDescription() const override
	{
		return TEXT("Create a new C++ class file (.h/.cpp) in the project's main game module.\n"
		"\n"
		"- Generates header and source from Unreal's standard class wizard templates.\n"
		"- Updates the module's Build.cs and regenerates IDE project files.\n"
		"- Does NOT compile. After creation, the user (or a separate tool) must trigger Live Coding (Ctrl+Alt+F11) to build.\n"
		"- `class_name` MUST include the UE prefix:\n"
		"    - 'A*' for AActor-derived (e.g. 'AEnemySpawner')\n"
		"    - 'U*' for UObject-derived (e.g. 'UInventoryComponent', 'UMainMenuWidget')\n"
		"    - 'F*' for plain structs / non-UObject\n"
		"- `parent_class` MUST be the full type name with prefix (e.g. 'AActor', not 'Actor').\n"
		"  Common parents: 'AActor', 'APawn', 'ACharacter', 'UObject', 'UActorComponent',\n"
		"  'USceneComponent', 'UUserWidget', 'UDataAsset', 'UGameInstance'.\n"
		"- `path` is optional. Use it to organize files under subfolders\n"
		"  (e.g. 'Gameplay', 'UI/Widgets'). Leave empty to place at the module root.\n"
		"- Header (.h) and source (.cpp) are generated in the same folder (no Public/Private split).\n"
		"- Returns the absolute paths of the generated files.\n"
		"## Folder organization\n"
		"When the user doesn't specify a location, choose `path` based on the class type:\n"
		"- AActor  → 'Actors' (or 'Actors/<Subcategory>')\n"
		"- APawn / ACharacter descendants → 'Character'\n"
		"- UActorComponent / USceneComponent descendants → 'Components'\n"
		"- UUserWidget descendants → 'UI/Widgets'\n"
		"- UDataAsset descendants → 'Data'\n"
		"- UGameInstance / UGameModeBase / APlayerController → 'Core'\n"
		"- AI-related (UBTTask, UBTService, AAIController) → 'AI'\n"
		"- Plain UObject utilities → 'Utility' or omit `path`\n"
		"When the user gives a hint (e.g. 'combat actor', 'inventory UI'),\n"
		"use a more specific subfolder like 'Actors/Combat' or 'UI/Inventory'.\n"
		"## Adding members and methods (header_body / cpp_body)\n"
        "Both are optional free-form C++ blocks. Use them when the user wants methods or properties.\n"
        "\n"
        "`header_body` — injected inside the class body (after GENERATED_BODY, before closing brace).\n"
        "  Each line is auto-indented one tab.\n"
        "  Put: UPROPERTY/UFUNCTION macros, member variables, function declarations,\n"
        "       access specifiers (public:/protected:/private:), virtual overrides.\n"
        "  Override BeginPlay/Tick: declare with `virtual void BeginPlay() override;`\n"
        "\n"
        "`cpp_body` — appended at end of the .cpp file (after the constructor).\n"
        "  Inserted verbatim, no indentation applied. Write fully-qualified definitions:\n"
        "  `void AMyActor::Attack() { ... }`\n"
        "  Always use the prefixed class name (AMyActor, not MyActor) in qualifiers.\n"
        "\n"
        "When you write methods, ALWAYS put the declaration in `header_body` AND the\n"
        "definition in `cpp_body`. Do not declare without defining (causes link errors).\n"
        "## Indentation\n"
        "DO NOT pre-indent `header_body`. Write each declaration starting at column 0.\n"
        "The tool auto-applies one tab to every non-empty line. Bad indentation in the\n"
        "input will be normalized (common leading whitespace is stripped first).\n"
        "\n"
        "## Includes\n"
        "When `header_body` or `cpp_body` references types beyond the parent class,\n"
        "add the required headers via `header_includes` / `cpp_includes`.\n"
        "Common cases:\n"
        "- TimerHandle, FTimerHandle members → no extra include (in CoreMinimal)\n"
        "- UStaticMeshComponent → header_includes: 'Components/StaticMeshComponent.h'\n"
        "- GetWorldTimerManager(), SpawnActor → cpp_includes: 'Engine/World.h'\n"
		"- UGameplayStatics → cpp_includes: 'Kismet/GameplayStatics.h'\n"
		"- UE_LOG → cpp_includes: 'Logging/LogMacros.h' (usually not needed via CoreMinimal)\n"
        "Format: one include per line, bare path preferred ('Path/Type.h').\n"
        "\n"
        "## Example with methods\n"
        "```json\n"
        "{\n"
        "  \"class_name\": \"AEnemySpawner\",\n"
        "  \"parent_class\": \"AActor\",\n"
        "  \"header_body\": \"public:\\n\\tvirtual void BeginPlay() override;\\n\\n\\tUFUNCTION(BlueprintCallable)\\n\\tvoid SpawnEnemy();\\n\\nprotected:\\n\\tUPROPERTY(EditAnywhere, Category=\\\"Spawning\\\")\\n\\tTSubclassOf<AActor> EnemyClass;\\n\\n\\tUPROPERTY(EditAnywhere, Category=\\\"Spawning\\\")\\n\\tfloat SpawnInterval = 2.0f;\",\n"
        "  \"cpp_body\": \"void AEnemySpawner::BeginPlay()\\n{\\n\\tSuper::BeginPlay();\\n\\tGetWorldTimerManager().SetTimer(SpawnTimer, this, &AEnemySpawner::SpawnEnemy, SpawnInterval, true);\\n}\\n\\nvoid AEnemySpawner::SpawnEnemy()\\n{\\n\\tif (EnemyClass)\\n\\t{\\n\\t\\tGetWorld()->SpawnActor<AActor>(EnemyClass, GetActorTransform());\\n\\t}\\n}\"\n"
        "}\n"
        "```\n"
        "\n"
		"## Example\n"
		"```json\n"
		"{\n"
		"  \"class_name\": \"AEnemySpawner\",\n"
		"  \"parent_class\": \"AActor\",\n"
		"  \"path\": \"Gameplay/Spawning\"\n"
		"}\n"
		"```\n"
		"\n"
		"## Errors\n"
		"- Returns failure if `class_name` already exists in the target path.\n"
		"- Returns failure if `parent_class` cannot be resolved (check prefix and spelling).");
	}
	
	virtual FMcpResponse Execute() override;

private:
	/** 풀네임 문자열("AActor", "UObject" 등)을 UClass*로 해석합니다 */
	UClass* FindParentClassByName(const FString& Name) const;
	
	/** UClass*에서 부모의 #include 경로를 추출 (예: AActor → "GameFramework/Actor.h") */
	FString ResolveParentIncludePath(UClass* InClass) const;
	
	/** Path가 비었을 때 부모 클래스로 폴더를 자동 결정 (B fallback) */
	FString ResolveAutoFolder(UClass* InClass) const;
};
