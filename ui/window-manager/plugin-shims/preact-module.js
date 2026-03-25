/**
 * Preact module shim for IIFE-isolated plugins.
 * When plugins import from "preact", esbuild aliases to this file,
 * which re-exports from the framework's shared globalThis.__preact instance.
 */
var __p = globalThis.__preact;
export var h = __p.h;
export var Fragment = __p.Fragment;
export var createElement = __p.createElement;
export var render = __p.render;
export var createRef = __p.createRef;
export var isValidElement = __p.isValidElement;
export var Component = __p.Component;
export var cloneElement = __p.cloneElement;
export var createContext = __p.createContext;
export var toChildArray = __p.toChildArray;
export var options = __p.options;
export default __p;
