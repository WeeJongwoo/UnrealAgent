// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class UnrealAgent : ModuleRules
{
	public UnrealAgent(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicIncludePaths.Add(ModuleDirectory);

        PublicDependencyModuleNames.AddRange([
            "Core",
            "CoreUObject",
            "Engine"
        ]);

        PrivateDependencyModuleNames.AddRange([
            "Slate",
            "SlateCore",
            "InputCore",
            "ApplicationCore",
            "UnrealEd",
            "EditorFramework",
            "EditorSubsystem",
            "LevelEditor",
            "ToolMenus",
            "StatusBar",
            "Projects",
            "WebBrowser",
            "Json",
            "JsonUtilities",
            "HTTPServer",
            "PythonScriptPlugin",
            "DesktopPlatform",
            "ImageWrapper",
			"LiveCoding",
            "UMG",
            "GameProjectGeneration",
            "SourceCodeAccess"
        ]);
    }
}
