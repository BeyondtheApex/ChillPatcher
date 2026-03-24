package main

import (
	"os"
	"path/filepath"
	"regexp"
	"strings"

	"github.com/evanw/esbuild/pkg/api"
)

var (
	// import { Foo, Bar } from "MyModule";
	importRegex = regexp.MustCompile(
		`import\s+(?:\{([^}]+)\})?\s*from\s*["']([A-Z][^"']*)["'];?`)
	// __require("MyModule")
	requireRegex = regexp.MustCompile(
		`__require\(["']([A-Z][^"']*)["']\)`)
)

// importTransformPlugin reimplements the onejs-core importTransformationPlugin in Go.
// 1. onResolve: marks imports starting with a capital letter as external
// 2. onLoad: rewrites `import { X } from "Mod"` → `const { X } = CS.Mod;`
func importTransformPlugin() api.Plugin {
	return api.Plugin{
		Name: "onejs-import-transform",
		Setup: func(build api.PluginBuild) {
			// Mark capitalized imports as external
			build.OnResolve(api.OnResolveOptions{Filter: `^[A-Z]`},
				func(args api.OnResolveArgs) (api.OnResolveResult, error) {
					return api.OnResolveResult{
						Path:     args.Path,
						External: true,
					}, nil
				})

			// Transform import statements in source files
			build.OnLoad(api.OnLoadOptions{Filter: `\.(js|jsx|ts|tsx)$`},
				func(args api.OnLoadArgs) (api.OnLoadResult, error) {
					data, err := os.ReadFile(args.Path)
					if err != nil {
						return api.OnLoadResult{}, err
					}
					contents := string(data)

					// Transform: import { Foo, Bar } from "MyModule"
					//         → const { Foo, Bar } = CS.MyModule;
					contents = importRegex.ReplaceAllStringFunc(contents, func(match string) string {
						sub := importRegex.FindStringSubmatch(match)
						if len(sub) < 3 {
							return match
						}
						imports := strings.TrimSpace(sub[1])
						moduleName := strings.ReplaceAll(sub[2], "/", ".")

						if imports != "" {
							items := strings.Split(imports, ",")
							for i := range items {
								items[i] = strings.TrimSpace(items[i])
							}
							return "const { " + strings.Join(items, ", ") + " } = CS." + moduleName + ";"
						}
						// Default/namespace import
						parts := strings.Split(moduleName, ".")
						name := parts[len(parts)-1]
						return "const " + name + " = CS." + moduleName + ";"
					})

					// Transform: __require("MyModule") → CS.MyModule
					contents = requireRegex.ReplaceAllStringFunc(contents, func(match string) string {
						sub := requireRegex.FindStringSubmatch(match)
						if len(sub) < 2 {
							return match
						}
						return "CS." + strings.ReplaceAll(sub[1], "/", ".")
					})

					// Determine loader from extension
					ext := filepath.Ext(args.Path)
					var loader api.Loader
					switch ext {
					case ".tsx":
						loader = api.LoaderTSX
					case ".ts":
						loader = api.LoaderTS
					case ".jsx":
						loader = api.LoaderJSX
					default:
						loader = api.LoaderJS
					}

					return api.OnLoadResult{
						Contents: &contents,
						Loader:   loader,
					}, nil
				})
		},
	}
}
