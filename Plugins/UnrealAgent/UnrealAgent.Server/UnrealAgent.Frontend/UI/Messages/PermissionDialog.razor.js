let controller;

/**
 * 1/2/3 키 입력을 감지하여 .NET으로 전달합니다.
 * textarea/input 포커스 중에는 무시합니다.
 */
export function setup(dotNetRef)
{
    // 이전 리스너 정리
    controller?.abort();
    controller = new AbortController();

    document.addEventListener("keydown", (e) =>
    {
        if (["INPUT", "TEXTAREA"].includes(document.activeElement?.tagName))
            return;

        if (["1", "2", "3"].includes(e.key))
        {
            e.preventDefault();
            dotNetRef.invokeMethodAsync("HandlePermissionKey", e.key);
        }
    }, { signal: controller.signal });
}