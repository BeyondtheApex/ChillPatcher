/**
 * Minimal onejs-core shim for IIFE-isolated plugins.
 * The framework's inject already initializes globalThis.document etc.
 * This shim just re-exports what plugins might need from onejs-core
 * without re-initializing the DOM wrapper.
 */
