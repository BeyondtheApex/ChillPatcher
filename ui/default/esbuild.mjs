/**
 * ChillPatcher OneJS ESbuild Config
 */
import * as esbuild from "esbuild"
import { importTransformationPlugin } from "onejs-core/scripts/esbuild/import-transform.mjs"

const once = process.argv.includes("--once")

let ctx = await esbuild.context({
    entryPoints: ["index.tsx"],
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
    outfile: "@outputs/esbuild/app.js",
    jsx: "transform",
    jsxFactory: "h",
    jsxFragment: "Fragment",
})

if (once) {
    await ctx.rebuild()
    await ctx.dispose()
    console.log("Build finished.")
    process.exit(0)
} else {
    await ctx.watch()
    console.log("Watching for changes…")
}
