/**
 * Preact hooks shim for IIFE-isolated plugins.
 * Re-exports from the framework's global preact hooks instance.
 */
var __ph = globalThis.__preactHooks;
var useState = __ph.useState;
var useEffect = __ph.useEffect;
var useCallback = __ph.useCallback;
var useMemo = __ph.useMemo;
var useRef = __ph.useRef;
var useErrorBoundary = __ph.useErrorBoundary;
var useReducer = __ph.useReducer;
var useContext = __ph.useContext;
var useLayoutEffect = __ph.useLayoutEffect;
var useImperativeHandle = __ph.useImperativeHandle;
var useDebugValue = __ph.useDebugValue;
