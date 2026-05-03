#pragma once

#include "CoreMinimal.h"
#include "Mcp/McpTypes.h"
#include "ExecutePythonTool.generated.h"

/**
 * 인라인 Python 코드를 실행하는 MCP 도구입니다
 *
 * tools/call로 Python 코드를 전달하면 GEditor 트랜잭션으로 래핑하여
 * 실행하고, 실패 시 자동 롤백합니다. Undo/Redo를 지원합니다.
 *
 * MCP 요청 예시:
 * {
 *     "jsonrpc": "2.0", "id": 1,
 *     "method": "tools/call",
 *     "params": {
 *         "name": "execute_python",
 *         "arguments": {
 *             "code": "import unreal\nprint('hello')",
 *             "purpose": "테스트 출력"
 *         }
 *     }
 * }
 */
USTRUCT(meta=(McpTool="execute_python"))
struct FExecutePythonTool : public FMcpTool
{
	GENERATED_BODY()

	/** 실행할 Python 코드 */
	UPROPERTY(meta=(ToolParam="code", Required,
					Description="The Python code to execute"))
	FString Code;
	
	/** 코드가 수행하는 작업에 대한 짧은 한국어 설명 */
	UPROPERTY(meta=(ToolParam="purpose", Required,
					Description="A short Korean description of what the code does (e.g. '월드에서 액터 검색', '머티리얼 속성 변경')"))
	FString Purpose;
	
	virtual FString ToolDescription() const override
	{
		return TEXT(
			"Execute Python code in Unreal Editor.\n"
			"\n"
			"- `import unreal` to access the editor API.\n"
			"- `print()` is your only way to see results. Code without print() returns nothing.\n"
			"- If unsure about available API, use `dir()` or `help()` to inspect at runtime.\n"
			"- Use unreal types (unreal.Vector, unreal.Rotator, unreal.Color, etc.)\n"
			"  instead of custom implementations.\n"
			"- Always use `set_editor_property()` / `get_editor_property()` instead of\n"
			"  direct property access. Direct access skips editor pre/post edit callbacks.\n"
			"- For asset operations, always use `unreal.EditorAssetLibrary` or\n"
			"  `unreal.AssetTools`. Never use Python file modules (os, shutil) on\n"
			"  asset files — this breaks internal content references.\n"
			"- CRITICAL: ANY loop (for, while, list comprehension iterating actors/assets)\n"
			"  MUST be wrapped in `unreal.ScopedSlowTask`. No exceptions. Unwrapped loops\n"
			"  will freeze the editor with no way to cancel.\n"
			"- Wrapped in a transaction for Undo support.\n"
			"- When querying actors, filter by class or name pattern first.\n"
			"  Never print all actors in a large level — summarize counts and\n"
			"  ask the user to narrow down.\n"
			"- When inspecting properties, print only the relevant ones,\n"
			"  not the full dir() output.\n"
			"\n"
			"## Basic patterns\n"
			"```python\n"
			"# Spawn actor\n"
			"actor = unreal.EditorLevelLibrary.spawn_actor_from_class(\n"
			"    unreal.StaticMeshActor, unreal.Vector(0, 0, 0))\n"
			"\n"
			"# List all actors\n"
			"actors = unreal.EditorLevelLibrary.get_all_level_actors()\n"
			"\n"
			"# Find actor by name\n"
			"actor = next((a for a in unreal.EditorLevelLibrary.get_all_level_actors()\n"
			"              if a.get_actor_label() == \"Cube_1\"), None)\n"
			"\n"
			"# Move / Rotate / Scale\n"
			"actor.set_actor_location(unreal.Vector(x, y, z))\n"
			"actor.set_actor_rotation(unreal.Rotator(pitch, yaw, roll))\n"
			"actor.set_actor_scale3d(unreal.Vector(sx, sy, sz))\n"
			"\n"
			"# Set properties (always use set_editor_property)\n"
			"component = actor.static_mesh_component\n"
			"component.set_editor_property(\"cast_shadow\", False)\n"
			"\n"
			"# Delete actor\n"
			"unreal.EditorLevelLibrary.destroy_actor(actor)\n"
			"\n"
			"# Load asset\n"
			"asset = unreal.EditorAssetLibrary.load_asset('/Game/Path/To/Asset')\n"
			"\n"
			"# Batch with progress bar + cancel\n"
			"with unreal.ScopedSlowTask(len(actors), \"Processing...\") as task:\n"
			"    task.make_dialog(True)\n"
			"    processed = 0\n"
			"    for actor in actors:\n"
			"        if task.should_cancel():\n"
			"            print(f\"Cancelled. {processed}/{len(actors)} done.\")\n"
			"            break\n"
			"        task.enter_progress_frame(1)\n"
			"        processed += 1\n"
			"        # ... work ...\n"
			"```"
		);
	}
	
	virtual FMcpResponse Execute() override;

private:
	/** LogOutput에서 Info 타입 메시지를 결합합니다 */
	FString ExtractOutput(const TArray<struct FPythonLogOutputEntry>& LogOutput) const;
};