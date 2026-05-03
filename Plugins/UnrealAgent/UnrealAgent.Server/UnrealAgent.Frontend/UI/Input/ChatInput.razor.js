/**
 * textarea에 키 바인딩을 설정합니다.
 * Enter → 전송, Shift+Tab → 모드 순환.
 * Shift+Enter는 줄바꿈을 유지합니다.
 */
export function setupKeyBindings(textarea, dotNetRef)
{
    textarea.addEventListener("keydown", function (e)
    {
        if (e.key === "Enter" && !e.shiftKey)
        {
            e.preventDefault();
            textarea.closest("form").requestSubmit();
        }
        else if (e.key === "Tab" && e.shiftKey)
        {
            e.preventDefault();
            dotNetRef.invokeMethodAsync("CycleMode");
        }
    });
}