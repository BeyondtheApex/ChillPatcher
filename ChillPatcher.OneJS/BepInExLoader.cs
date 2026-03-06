using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Puerts;

namespace OneJS
{
    /// <summary>
    /// Custom ILoader for BepInEx environment.
    /// PuerTS bootstrap scripts (puerts/*.mjs) are loaded from embedded resources.
    /// User scripts are loaded from the file system.
    /// </summary>
    public class BepInExLoader : ILoader, IModuleChecker
    {
        private readonly string _root;
        private readonly Dictionary<string, string> _bootstrapCache = new Dictionary<string, string>();
        private readonly Assembly _assembly;

        public BepInExLoader(string root)
        {
            _root = root ?? "";
            _assembly = typeof(BepInExLoader).Assembly;
            LoadBootstrapScripts();
        }

        private void LoadBootstrapScripts()
        {
            var names = _assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                // Embedded resource names: "puerts.<filename>.mjs" or "puerts.<filename>.cjs"
                if (!name.StartsWith("puerts.", StringComparison.Ordinal)) continue;

                // Convert "puerts.init.mjs" -> "puerts/init.mjs"
                var idx = name.IndexOf('.');
                var filePart = name.Substring(idx + 1); // "init.mjs"
                var key = "puerts/" + filePart;

                using (var stream = _assembly.GetManifestResourceStream(name))
                {
                    if (stream == null) continue;
                    using (var reader = new StreamReader(stream))
                    {
                        _bootstrapCache[key] = reader.ReadToEnd();
                    }
                }

                // Also store without extension for PuerTS PathToUse stripping
                // PuerTS strips .mjs/.cjs extensions when calling FileExists/ReadFile
                if (filePart.EndsWith(".mjs") || filePart.EndsWith(".cjs"))
                {
                    var keyNoExt = "puerts/" + filePart.Substring(0, filePart.Length - 4);
                    _bootstrapCache[keyNoExt] = _bootstrapCache[key];
                }
            }
        }

        public bool FileExists(string filepath)
        {
            // Check bootstrap cache first
            if (_bootstrapCache.ContainsKey(filepath))
                return true;

            // Then check file system
            var fullPath = Path.Combine(_root, filepath);
            return File.Exists(fullPath);
        }

        public string ReadFile(string filepath, out string debugpath)
        {
            // Check bootstrap cache first
            if (_bootstrapCache.TryGetValue(filepath, out var content))
            {
                debugpath = filepath;
                return content;
            }

            // Read from file system
            debugpath = Path.Combine(_root, filepath);
            if (File.Exists(debugpath))
                return File.ReadAllText(debugpath);

            return null;
        }

        public bool IsESM(string filepath)
        {
            return filepath.Length >= 4 && !filepath.EndsWith(".cjs");
        }
    }
}
