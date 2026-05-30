/**
 * Preact hooks module shim for IIFE-isolated plugins.
 * When plugins import from "preact/hooks", esbuild aliases to this file,
 * which re-exports from the framework's shared globalThis.__preactHooks instance.
 */
var __ph = globalThis.__preactHooks;
export var useState = __ph.useState;
export var useEffect = __ph.useEffect;
export var useCallback = __ph.useCallback;
export var useMemo = __ph.useMemo;
export var useRef = __ph.useRef;
export var useErrorBoundary = __ph.useErrorBoundary;
export var useReducer = __ph.useReducer;
export var useContext = __ph.useContext;
export var useLayoutEffect = __ph.useLayoutEffect;
export var useImperativeHandle = __ph.useImperativeHandle;
export var useDebugValue = __ph.useDebugValue;
export var useEventfulState = __ph.useEventfulState;
export default __ph;
