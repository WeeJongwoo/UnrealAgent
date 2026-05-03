#include "CreateCppClassTool.h"

#include "GameProjectUtils.h"
#include "GameProjectGenerationModule.h"
#include "ModuleDescriptor.h"
#include "UObject/Class.h"
#include "UObject/UObjectGlobals.h"
#include "SourceCodeNavigation.h"        // 부모 클래스의 헤더 경로 자동 추출
#include "GameFramework/Actor.h"          // ParentUClass->IsChildOf(AActor) 비교용
#include "Components/ActorComponent.h"

#include UE_INLINE_GENERATED_CPP_BY_NAME(CreateCppClassTool)

namespace
{
	/**
     * 공통 leading whitespace 제거(dedent) → 정확히 NewIndent를 비어있지 않은 라인에 적용.
     * 입력의 LLM별 들여쓰기 편차를 무시하고 출력 일관성 보장.
     */
    FString DedentAndReindent(FString Body, const TCHAR* NewIndent)
    {
        if (Body.IsEmpty()) return Body;

        Body.ReplaceInline(TEXT("\r\n"), TEXT("\n"));
        Body.ReplaceInline(TEXT("\r"), TEXT("\n"));

        TArray<FString> Lines;
        Body.ParseIntoArray(Lines, TEXT("\n"), /*CullEmpty*/false);

        // 비어있지 않고 공백만으로도 안 채워진 라인의 최소 leading whitespace 산출
        int32 MinIndent = TNumericLimits<int32>::Max();
        for (const FString& Line : Lines)
        {
            int32 Leading = 0;
            bool bHasContent = false;
            for (int32 i = 0; i < Line.Len(); ++i)
            {
                const TCHAR C = Line[i];
                if (C == TEXT(' ') || C == TEXT('\t')) { ++Leading; }
                else { bHasContent = true; break; }
            }
            if (bHasContent)
            {
                MinIndent = FMath::Min(MinIndent, Leading);
            }
        }
        if (MinIndent == TNumericLimits<int32>::Max()) MinIndent = 0;

        const bool bAddIndent = NewIndent && *NewIndent != TEXT('\0');
        FString Out;
        for (int32 i = 0; i < Lines.Num(); ++i)
        {
            FString Line = Lines[i];

            // dedent: 공통 leading whitespace 제거
            if (Line.Len() >= MinIndent)
            {
                bool bIsWs = true;
                for (int32 j = 0; j < MinIndent; ++j)
                {
                    if (Line[j] != TEXT(' ') && Line[j] != TEXT('\t')) { bIsWs = false; break; }
                }
                if (bIsWs) Line = Line.Mid(MinIndent);
            }

            if (bAddIndent && !Line.IsEmpty())
            {
                Out += NewIndent;
            }
            Out += Line;
            if (i < Lines.Num() - 1) Out += TEXT("\r\n");
        }
        return Out;
    }

    /**
     * 줄바꿈/쉼표 구분 include 입력을 정식 #include 라인으로 변환.
     * 각 항목은 'Path/Type.h' 또는 '#include "Path/Type.h"' 둘 다 허용.
     */
    FString FormatIncludeLines(const FString& InRaw)
    {
        if (InRaw.IsEmpty()) return FString();

        FString Body = InRaw;
        Body.ReplaceInline(TEXT("\r\n"), TEXT("\n"));
        Body.ReplaceInline(TEXT("\r"), TEXT("\n"));
        Body.ReplaceInline(TEXT(","), TEXT("\n"));

        TArray<FString> Lines;
        Body.ParseIntoArray(Lines, TEXT("\n"), /*CullEmpty*/true);

        FString Out;
        for (FString Line : Lines)
        {
            Line.TrimStartAndEndInline();
            if (Line.IsEmpty()) continue;

            if (Line.StartsWith(TEXT("#include")))
            {
                Out += Line + TEXT("\r\n");
            }
            else
            {
                // 양쪽 따옴표/꺾쇠 정리
                Line.RemoveFromStart(TEXT("\""));
                Line.RemoveFromEnd(TEXT("\""));
                Line.RemoveFromStart(TEXT("<"));
                Line.RemoveFromEnd(TEXT(">"));
                Out += FString::Printf(TEXT("#include \"%s\"\r\n"), *Line);
            }
        }
        return Out;
    }
}

FMcpResponse FCreateCppClassTool::Execute()
{
	// 1) 입력 검증
	if (ClassName.IsEmpty())
	{
		return FMcpResponse::Failure(TEXT("class_name is required"));
	}
	if (ParentClass.IsEmpty())
	{
		return FMcpResponse::Failure(TEXT("parent_class is required"));
	}

	// 2) 부모 클래스 해석
	UClass* ParentUClass = FindParentClassByName(ParentClass);
	if (!ParentUClass)
	{
		return FMcpResponse::Failure(FString::Printf(
			TEXT("Unknown parent_class: '%s'. Use the full UE type name with prefix (e.g. 'AActor', 'UObject')."),
			*ParentClass));
	}

	// 3) 메인 게임 모듈 찾기
    FGameProjectGenerationModule& GameProjectModule =
        FModuleManager::LoadModuleChecked<FGameProjectGenerationModule>(TEXT("GameProjectGeneration"));

    const TArray<FModuleContextInfo>& Modules = GameProjectModule.GetCurrentProjectModules();
    if (Modules.Num() == 0)
    {
        return FMcpResponse::Failure(TEXT("No game modules found in the current project. Project must have at least one C++ module."));
    }

    // 첫 번째 모듈 = 프로젝트 메인 게임 모듈 (.uproject의 Modules[0])
    const FModuleContextInfo& MainModule = Modules[0];
	
	// 4.0) Path가 비어있으면 부모 클래스로 자동 폴더 결정 (B fallback)
	FString EffectivePath = Path;
	if (EffectivePath.IsEmpty())
	{
		EffectivePath = ResolveAutoFolder(ParentUClass);
		if (!EffectivePath.IsEmpty())
		{
			UE_LOG(LogTemp, Display,
				TEXT("[create_cpp_class] Auto-resolved path: '%s' (parent=%s)"),
				*EffectivePath, *ParentUClass->GetName());
		}
	}

	// 4) 출력 디렉토리
    FString TargetDir = MainModule.ModuleSourcePath;
    if (!Path.IsEmpty())
    {
        FString CleanSubPath = Path;
        CleanSubPath.ReplaceInline(TEXT("\\"), TEXT("/"));
        CleanSubPath.RemoveFromStart(TEXT("/"));
        CleanSubPath.RemoveFromEnd(TEXT("/"));
        TargetDir = FPaths::Combine(TargetDir, CleanSubPath);
    }
    if (!TargetDir.EndsWith(TEXT("/")))
    {
        TargetDir += TEXT("/");
    }

    // 5) 클래스 이름 정규화 (접두사 보장)
    //    ParentClass 종류에 따라 A* / U* 자동 결정
    FString FullClassName = ClassName;
    {
        const TCHAR First = FullClassName.IsEmpty() ? TEXT('\0') : FullClassName[0];
        const bool bHasPrefix = FullClassName.Len() > 1 &&
            (First == TEXT('A') || First == TEXT('U') || First == TEXT('F')) &&
            FChar::IsUpper(FullClassName[1]);

        if (!bHasPrefix)
        {
            const FString Prefix = ParentUClass->IsChildOf(AActor::StaticClass()) ? TEXT("A") : TEXT("U");
            FullClassName = Prefix + FullClassName;
        }
    }

	// 5.5) 파일명용 이름 (접두사 제거)
	FString FileBaseName = FullClassName;
	if (FileBaseName.Len() > 1)
	{
		const TCHAR First = FileBaseName[0];
		if ((First == TEXT('A') || First == TEXT('U') || First == TEXT('F'))
			&& FChar::IsUpper(FileBaseName[1]))
		{
			FileBaseName = FileBaseName.Mid(1);
		}
	}

	// 6) 파일 경로 + 중복 체크 (FileBaseName 사용)
	const FString HeaderPath = TargetDir + FileBaseName + TEXT(".h");
	const FString CppPath    = TargetDir + FileBaseName + TEXT(".cpp");

    if (IFileManager::Get().FileExists(*HeaderPath))
        return FMcpResponse::Failure(FString::Printf(TEXT("Already exists: %s"), *HeaderPath));
    if (IFileManager::Get().FileExists(*CppPath))
        return FMcpResponse::Failure(FString::Printf(TEXT("Already exists: %s"), *CppPath));

    // 7) 디렉토리 보장
    IFileManager::Get().MakeDirectory(*TargetDir, /*Tree*/true);

	 // 8) 템플릿 생성
    const FString ApiMacro       = MainModule.ModuleName.ToUpper() + TEXT("_API");
    const FString ParentInclude  = ResolveParentIncludePath(ParentUClass);
    const FString ParentTypeName = ParentClass;

    // 본문 정규화 (dedent + 한 탭 reindent)
    FString HeaderInjection;
    if (!HeaderBody.IsEmpty())
    {
        HeaderInjection = TEXT("\r\n")
                        + DedentAndReindent(HeaderBody, TEXT("\t"))
                        + TEXT("\r\n");
    }

    FString CppInjection;
    if (!CppBody.IsEmpty())
    {
        // cpp_body는 함수 정의들이라 인덴트 안 함 (각 함수 내부 인덴트는 LLM 책임)
        CppInjection = TEXT("\r\n")
                     + DedentAndReindent(CppBody, nullptr)
                     + TEXT("\r\n");
    }

    // 추가 include 포맷팅
    const FString HeaderExtraIncludes = FormatIncludeLines(HeaderIncludes);
    const FString CppExtraIncludes    = FormatIncludeLines(CppIncludes);

    const FString HeaderContent = FString::Printf(TEXT(
        "// Auto-generated by UnrealAgent\r\n"
        "#pragma once\r\n"
        "\r\n"
        "#include \"CoreMinimal.h\"\r\n"
        "#include \"%s\"\r\n"
        "%s"  // header_includes (있으면 줄 단위로 이미 \r\n 포함)
        "#include \"%s.generated.h\"\r\n"
        "\r\n"
        "UCLASS()\r\n"
        "class %s %s : public %s\r\n"
        "{\r\n"
        "\tGENERATED_BODY()\r\n"
        "\r\n"
        "public:\r\n"
        "\t%s();\r\n"
        "%s"  // HeaderInjection
        "};\r\n"),
        *ParentInclude,
        *HeaderExtraIncludes,
        *FileBaseName,
        *ApiMacro, *FullClassName, *ParentTypeName,
        *FullClassName, *HeaderInjection);

    const FString CppContent = FString::Printf(TEXT(
        "// Auto-generated by UnrealAgent\r\n"
        "#include \"%s.h\"\r\n"
        "%s"  // cpp_includes
        "\r\n"
        "%s::%s()\r\n"
        "{\r\n"
        "}\r\n"
        "%s"),  // CppInjection
        *FileBaseName,
        *CppExtraIncludes,
        *FullClassName, *FullClassName,
        *CppInjection);

    // 9) 디스크 쓰기 (UTF-8 BOM 없이 — UE 컨벤션)
    if (!FFileHelper::SaveStringToFile(HeaderContent, *HeaderPath,
            FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
    {
        return FMcpResponse::Failure(FString::Printf(TEXT("Failed to write: %s"), *HeaderPath));
    }
    if (!FFileHelper::SaveStringToFile(CppContent, *CppPath,
            FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
    {
        return FMcpResponse::Failure(FString::Printf(TEXT("Failed to write: %s"), *CppPath));
    }
	
	UE_LOG(LogTemp, Warning, TEXT("[create_cpp_class] DONE: returning success for %s"), *FullClassName);

    return FMcpResponse::Success(FString::Printf(
        TEXT("Created %s : %s\n  Header: %s\n  Source: %s\n\nNext steps:\n"
             "1) Right-click .uproject → 'Generate Visual Studio project files'\n"
             "2) Live Coding (Ctrl+Alt+F11) to compile"),
        *FullClassName, *ParentTypeName, *HeaderPath, *CppPath));
}

UClass* FCreateCppClassTool::FindParentClassByName(const FString& Name) const
{
	if (Name.IsEmpty())
	{
		return nullptr;
	}

	// UClass 리플렉션 이름은 C++ 접두사를 포함하지 않음 (AActor → "Actor", UObject → "Object")
	// 입력이 "AActor"처럼 접두사를 포함하면 떼고 검색

	// 1) 접두사 제거 시도: 첫 글자가 A/U/F이고 두 번째 글자가 대문자면 접두사로 간주
	//    "AActor"의 'A' (두 번째 'A'가 대문자) → 접두사
	//    "Actor"의 'A' (두 번째 'c'가 소문자) → 접두사 아님
	if (Name.Len() > 1)
	{
		const TCHAR First = Name[0];
		if ((First == TEXT('A') || First == TEXT('U') || First == TEXT('F'))
			&& FChar::IsUpper(Name[1]))
		{
			const FString Stripped = Name.Mid(1);
			if (UClass* Found = UClass::TryFindTypeSlow<UClass>(Stripped))
			{
				return Found;
			}
		}
	}

	// 2) 입력 그대로 시도 (이미 접두사 없는 입력 또는 접두사 제거가 부적절했던 경우)
	if (UClass* Found = UClass::TryFindTypeSlow<UClass>(Name))
	{
		return Found;
	}

	return nullptr;
}

FString FCreateCppClassTool::ResolveParentIncludePath(UClass* InClass) const
{
	// FSourceCodeNavigation으로 헤더 절대경로를 얻은 뒤 모듈 Public 폴더 기준 상대경로로 변환
	FString HeaderAbsPath;
	if (FSourceCodeNavigation::FindClassHeaderPath(InClass, HeaderAbsPath) && !HeaderAbsPath.IsEmpty())
	{
		// 절대경로에서 ".../Public/" 또는 ".../Classes/" 이후 부분만 사용
		const TCHAR* Markers[] = { TEXT("/Public/"), TEXT("/Classes/") };
		for (const TCHAR* Marker : Markers)
		{
			int32 Idx;
			if (HeaderAbsPath.FindLastChar(TEXT('/'), Idx) &&
				HeaderAbsPath.Contains(Marker))
			{
				const int32 Pos = HeaderAbsPath.Find(Marker, ESearchCase::IgnoreCase, ESearchDir::FromEnd);
				if (Pos != INDEX_NONE)
				{
					return HeaderAbsPath.Mid(Pos + FCString::Strlen(Marker));
				}
			}
		}
		// 마커 못 찾으면 파일명만
		return FPaths::GetCleanFilename(HeaderAbsPath);
	}

	// Fallback: 잘 알려진 엔진 클래스 매핑
	static const TMap<FString, FString> Known = {
		{ TEXT("Actor"),           TEXT("GameFramework/Actor.h") },
		{ TEXT("Pawn"),            TEXT("GameFramework/Pawn.h") },
		{ TEXT("Character"),       TEXT("GameFramework/Character.h") },
		{ TEXT("Object"),          TEXT("UObject/Object.h") },
		{ TEXT("ActorComponent"),  TEXT("Components/ActorComponent.h") },
		{ TEXT("SceneComponent"),  TEXT("Components/SceneComponent.h") },
		{ TEXT("UserWidget"),      TEXT("Blueprint/UserWidget.h") },
	};
	if (const FString* Found = Known.Find(InClass->GetName()))
	{
		return *Found;
	}
	return FString::Printf(TEXT("%s.h"), *InClass->GetName());
}

FString FCreateCppClassTool::ResolveAutoFolder(UClass* InClass) const
{
	if (!InClass) return FString();

	// 빠른 직접 비교 (가장 흔한 두 경우)
	if (InClass->IsChildOf(AActor::StaticClass()))           return TEXT("Actors");
	if (InClass->IsChildOf(UActorComponent::StaticClass()))  return TEXT("Components");

	// 나머지는 의존성 줄이려고 클래스 이름으로 비교 (계층 거슬러 올라가며)
	for (UClass* C = InClass; C; C = C->GetSuperClass())
	{
		const FString N = C->GetName();
		if (N == TEXT("UserWidget"))     return TEXT("UI/Widgets");
		if (N == TEXT("DataAsset"))      return TEXT("Data");
		if (N == TEXT("GameInstance"))   return TEXT("Core");
		if (N == TEXT("GameModeBase"))   return TEXT("Core");
		if (N == TEXT("PlayerController"))return TEXT("Core");
		if (N == TEXT("AIController"))   return TEXT("AI");
		if (N == TEXT("BTTaskNode"))     return TEXT("AI");
		if (N == TEXT("BTService"))      return TEXT("AI");
	}

	return FString(); // 기본: 모듈 루트
}