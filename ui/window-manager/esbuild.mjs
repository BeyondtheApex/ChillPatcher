/**
 * Window Manager - ESbuild Config
 * Builds the framework entry + all plugins in plugins/
 */
import * as esbuild from "esbuild"
import { importTransformationPlugin } from "onejs-core/scripts/esbuild/import-transform.mjs"
import { readdirSync, existsSync, watch } from "fs"
import { join, sep, normalize } from "path"

const once = process.argv.includes("--once")

const sharedOptions = {
    bundle: true,
    plugins: [importTransformationPlugin()],
    inject: ["node_modules/onejs-core/dist/index.js"],
    platform: "node",
    sourcemap: true,
    sourceRoot: process.cwd(),
    alias: {
        "onejs": "onejs-core",
        "preact": "onejs-preact",
        "react": "onejs-preact/compat",
        "react-dom": "onejs-preact/compat"
    },
    jsx: "transform",
    jsxFactory: "h",
    jsxFragment: "Fragment",
}

// ---- Framework ----
let frameworkCtx = await esbuild.context({
    ...sharedOptions,
    entryPoints: ["index.tsx"],
    outfile: "@outputs/esbuild/app.js",
})

// ---- Plugins ----
// 插件使用 IIFE 格式防止顶层变量泄漏到全局作用域，避免多插件间同名变量冲突。
// 但 IIFE 会导致 preact/onejs-core 被重复 bundle，产生独立实例，
// 与 framework 的 preact 不共享 hooks 状态，导致渲染异常。
// 解决方案：plugin 不 inject onejs-core，不 bundle preact，
// 而是通过 shim 文件从 framework 暴露的 globalThis.__preact 获取共享实例。
const pluginSharedOptions = {
    bundle: true,
    plugins: [importTransformationPlugin()],
    inject: [
        "plugin-shims/onejs-core.js",   // 空 shim（framework 已初始化 document）
    ],
    platform: "node",
    sourcemap: true,
    sourceRoot: process.cwd(),
    alias: {
        "onejs": "onejs-core",
        "preact": "./plugin-shims/preact-module.js",
        "preact/hooks": "./plugin-shims/preact-hooks-module.js",
        "react": "./plugin-shims/preact-module.js",
        "react-dom": "./plugin-shims/preact-module.js",
    },
    jsx: "transform",
    jsxFactory: "h",
    jsxFragment: "Fragment",
    format: "iife",
}

const pluginContexts = []
const pluginsDir = "plugins"

if (existsSync(pluginsDir)) {
    for (const name of readdirSync(pluginsDir, { withFileTypes: true })) {
        if (!name.isDirectory()) continue
        const entry = join(pluginsDir, name.name, "index.tsx")
        if (!existsSync(entry)) continue

        const ctx = await esbuild.context({
            ...pluginSharedOptions,
            entryPoints: [entry],
            outfile: `@outputs/plugins/${name.name}/app.js`,
        })
        pluginContexts.push({ name: name.name, ctx })
    }
}

if (once) {
    await frameworkCtx.rebuild()
    for (const p of pluginContexts) {
        await p.ctx.rebuild()
    }
    await frameworkCtx.dispose()
    for (const p of pluginContexts) {
        await p.ctx.dispose()
    }
    console.log(`Build finished. (framework + ${pluginContexts.length} plugin(s))`)
    process.exit(0)
} else {
    await frameworkCtx.watch()

    // 使用 fs.watch 递归监视 plugins 目录，手动触发插件重编译
    // esbuild 内置 watch 对独立 entry point 的插件目录可能无法可靠检测变更
    if (existsSync(pluginsDir) && pluginContexts.length > 0) {
        // 先做一次初始构建
        for (const p of pluginContexts) {
            await p.ctx.rebuild()
        }

        // 按插件名建立查找表
        const pluginMap = new Map()
        for (const p of pluginContexts) {
            pluginMap.set(p.name, p)
        }

        // 防抖计时器（每个插件独立防抖）
        const debounceTimers = new Map()
        const DEBOUNCE_MS = 200

        watch(pluginsDir, { recursive: true }, (eventType, filename) => {
            if (!filename) return
            // 忽略非源码文件
            if (!/\.(tsx?|jsx?|css|json)$/i.test(filename)) return

            // 从文件路径中提取插件名: "weather/index.tsx" -> "weather"
            const normalized = filename.replace(/\\/g, "/")
            const pluginName = normalized.split("/")[0]
            const plugin = pluginMap.get(pluginName)
            if (!plugin) return

            // 防抖：连续变更只触发一次重编译
            if (debounceTimers.has(pluginName)) {
                clearTimeout(debounceTimers.get(pluginName))
            }
            debounceTimers.set(pluginName, setTimeout(async () => {
                debounceTimers.delete(pluginName)
                try {
                    await plugin.ctx.rebuild()
                    console.log(`[${pluginName}] Rebuilt.`)
                } catch (e) {
                    console.error(`[${pluginName}] Build error:`, e.message)
                }
            }, DEBOUNCE_MS))
        })
    }

    console.log(`Watching for changes… (framework + ${pluginContexts.length} plugin(s))`)
}
