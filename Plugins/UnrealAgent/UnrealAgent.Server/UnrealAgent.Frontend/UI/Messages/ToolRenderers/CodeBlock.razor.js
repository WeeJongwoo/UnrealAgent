/**
 * 지정된 code 요소에 highlight.js를 적용합니다.
 */
export function highlightCode(element)
{
    if (element && window.hljs)
    {
        hljs.highlightElement(element);
    }
}