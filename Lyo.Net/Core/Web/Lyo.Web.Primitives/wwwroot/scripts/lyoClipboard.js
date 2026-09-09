export async function readText() {
    return await navigator.clipboard.readText();
}
