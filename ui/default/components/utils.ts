/**
 * Parse a JSON string returned by C# API methods.
 * All complex C# API returns (arrays, objects) are now JSON-serialized on the C# side.
 * JS side just needs JSON.parse() to get native objects/arrays.
 */
export function parse<T = any>(json: string): T {
    if (json === "null" || json == null) return null as T
    return JSON.parse(json) as T
}
