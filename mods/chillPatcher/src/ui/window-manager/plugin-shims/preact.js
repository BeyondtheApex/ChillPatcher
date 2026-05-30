/**
 * Preact shim for IIFE-isolated plugins.
 * Re-exports from the framework's global preact instance
 * so all plugins share a single preact runtime.
 */
var __p = globalThis.__preact;
var h = __p.h;
var Fragment = __p.Fragment;
var createElement = __p.createElement;
var render = __p.render;
var createRef = __p.createRef;
var isValidElement = __p.isValidElement;
var Component = __p.Component;
var cloneElement = __p.cloneElement;
var createContext = __p.createContext;
var toChildArray = __p.toChildArray;
var options = __p.options;
