using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using OneJS.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace OneJS {
    /// <summary>
    /// Sets up OneJS for first time use. It creates essential files in the WorkingDir if they are missing.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ScriptEngine))] [AddComponentMenu("OneJS/Bundler")]
    public class Bundler : MonoBehaviour {
        [PairMapping("path", "textAsset", ":")]
        public DefaultFileMapping[] defaultFiles;

        [Tooltip("Top-level files and directories you want to include in your deployment bundle.")] [PlainString]
        public string[] includes = new string[] { "@outputs", "assets" };

        [FormerlySerializedAs("outputsZip")]
        [Tooltip("The packaged bundle as a tarball. Will be automatically extracted for Standalone Player builds.")]
        public TextAsset bundleZip;

        [Tooltip("Deployment version for the Standalone Player. The bundled contents will be overridden if there's a version mismatch or \"Force Extract\" is on.")]
        public string version = "1.0";
        [Tooltip("Force extract on every game start, irregardless of version.")]
        public bool forceExtract;
        [Tooltip("Files and folders that you don't want to be packaged. Can use glob patterns.")] [PlainString]
        public string[] ignoreList = new string[] { "@outputs/tsc", "node_modules", "tmp" };

        ScriptEngine _engine;
        string _onejsVersion = "2.3.4";

        void Awake() {
            _engine = GetComponent<ScriptEngine>();
            var versionString = PlayerPrefs.GetString("ONEJS_VERSION", "0.0.0");
#if !UNITY_EDITOR && (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
            ExtractForStandalone();
#else
            if (versionString != _onejsVersion) {
                // DeleteEverythingInPath(Path.Combine(_engine.WorkingDir, "onejs-core"));
                // if (_extractSamples)
                //     ExtractSamples();

                PlayerPrefs.SetString("ONEJS_VERSION", _onejsVersion);
            }

            ExtractAll();
#endif
        }

        public void ExtractAll() {
            foreach (var mapping in defaultFiles) {
                CreateIfNotFound(mapping);
            }
            CreateVSCodeSettingsJsonIfNotFound();

            // WriteToPackageJson();
            // ExtractOnejsCoreIfNotFound();
            ExtractOutputsIfNotFound();
        }

        void CreateIfNotFound(DefaultFileMapping mapping) {
            var path = Path.Combine(_engine.WorkingDir, mapping.path);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(path)) {
                File.WriteAllText(path, mapping.textAsset.text);
                Debug.Log($"'{mapping.path}' wasn't found. A new one was created.");
            }
        }

        /// <summary>
        /// Not used anymore because of VCS issues when different users have different paths.
        /// </summary>
        void WriteToPackageJson() {
            string scriptPath = new StackTrace(true).GetFrame(0).GetFileName();
            var onejsPath = ParentFolder(ParentFolder(ParentFolder(scriptPath)));
            // var escapedOnejsPath = onejsPath.Replace(@"\", @"\\").Replace("\"", "\\\"");
            var packageJsonPath = Path.Combine(_engine.WorkingDir, "package.json");
            string jsonString = File.ReadAllText(packageJsonPath);
            Dictionary<string, object> jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            jsonDict["onejs"] = new Dictionary<string, object> {
                { "unity-package-path", onejsPath }
            };

            string updatedJsonString = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
            File.WriteAllText(packageJsonPath, updatedJsonString);
        }

        void CreateVSCodeSettingsJsonIfNotFound() {
            var path = Path.Combine(_engine.WorkingDir, ".vscode/settings.json");
            // Create if path doesn't exist
            if (!File.Exists(path)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>("{}");

                jsonDict["window.title"] = Application.productName;

                string updatedJsonString = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
                File.WriteAllText(path, updatedJsonString);
            }
        }

        string ParentFolder(string path) {
            return Directory.GetParent(path)?.FullName;
        }

        // public void ExtractOnejsCoreIfNotFound() {
        //     _engine = GetComponent<ScriptEngine>();
        //     var path = Path.Combine(_engine.WorkingDir, "onejs-core");
        //     if (Directory.Exists(path))
        //         return;
        //
        //     Extract(onejsCoreZip.bytes);
        //     Debug.Log($"An existing 'onejs-core' directory wasn't found. A new one was created ({path})");
        // }

        public void ExtractOutputsIfNotFound() {
            _engine = GetComponent<ScriptEngine>();
            var path = Path.Combine(_engine.WorkingDir, "@outputs");
            if (Directory.Exists(path))
                return;

            Extract(bundleZip.bytes);
            Debug.Log($"An existing 'outputs' directory wasn't found. An example one was created ({path})");
        }

        public void ExtractForStandalone() {
            var deployVersion = PlayerPrefs.GetString("ONEJS_APP_DEPLOYMENT_VERSION", "0.0");
            var outputPath = _engine.WorkingDir;
            if (forceExtract || deployVersion != version) {
                Debug.Log($"Extracting for Standalone Player. Deployment Version: {version}");
                if (Directory.Exists(outputPath))
                    DeleteEverythingInPath(outputPath);
                Extract(bundleZip.bytes);
                Debug.Log($"Bundle Zip extracted.");

                PlayerPrefs.SetString("ONEJS_APP_DEPLOYMENT_VERSION", version);
            }
        }

        void Extract(byte[] bytes) {
            Stream inStream = new MemoryStream(bytes);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(_engine.WorkingDir);
            tarArchive.Close();
            gzipStream.Close();
            inStream.Close();
        }

        /// <summary>
        /// Root folder at path still remains
        /// </summary>
        void DeleteEverythingInPath(string path) {
            var dotGitPath = Path.Combine(path, ".git");
            if (Directory.Exists(dotGitPath)) {
                Debug.Log($".git folder detected at {path}, aborting extraction.");
                return;
            }
            if (Directory.Exists(path)) {
                var di = new DirectoryInfo(path);
                foreach (FileInfo file in di.EnumerateFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
                    dir.Delete(true);
                }
            }
        }

#if UNITY_EDITOR

        // [ContextMenu("Package onejs-core.tgz")]
        // void PackageOnejsCoreZip() {
        //     _engine = GetComponent<ScriptEngine>();
        //     var t = DateTime.Now;
        //     var path = Path.Combine(_engine.WorkingDir, "onejs-core");
        //
        //     if (onejsCoreZip == null) {
        //         EditorUtility.DisplayDialog("onejs-core.tgz is null",
        //             "Please make sure you have a onejs-core.tgz (Text Asset) set", "Okay");
        //         return;
        //     }
        //     if (EditorUtility.DisplayDialog("Are you sure?",
        //             "This will package up your onejs-core folder under ScriptEngine.WorkingDir into a tgz file " +
        //             "and override your existing onejs-core.tgz file.",
        //             "Confirm", "Cancel")) {
        //         var binPath = AssetDatabase.GetAssetPath(onejsCoreZip);
        //         binPath = Path.GetFullPath(Path.Combine(Application.dataPath, @".." + Path.DirectorySeparatorChar, binPath));
        //         var outStream = File.Create(binPath);
        //         var gzoStream = new GZipOutputStream(outStream);
        //         gzoStream.SetLevel(3);
        //         var tarOutputStream = new TarOutputStream(gzoStream);
        //         var tarCreator = new TarCreator(path, _engine.WorkingDir) {
        //             IgnoreList = new[] { "**/node_modules", "**/.prettierrc", "**/jsr.json", "**/package.json", "**/tsconfig.json", "**/README.md" }
        //         };
        //         tarCreator.CreateTar(tarOutputStream);
        //
        //         Debug.Log($"onejs-core.tgz.bytes file updated. {tarOutputStream.Length} bytes {(DateTime.Now - t).TotalMilliseconds}ms");
        //         tarOutputStream.Close();
        //     }
        // }

        [ContextMenu("Package bundle.tgz")]
        public void PackageBundleWithPrompt() {
            if (bundleZip == null) {
                EditorUtility.DisplayDialog("bundle.tgz is null",
                    "Please make sure you have an bundle.tgz set", "Okay");
                return;
            }
            if (EditorUtility.DisplayDialog("Are you sure?",
                    "This will package up your WorkingDir into a tgz file (using your Includes and Ignore settings) " +
                    "and override your existing bundle.tgz file.",
                    "Confirm", "Cancel")) {
                PackageBundle();
            }
        }

        public void PackageBundle() {
            _engine = GetComponent<ScriptEngine>();
            var t = DateTime.Now;
            // var path = Path.Combine(_engine.WorkingDir, "@outputs");
            var binPath = AssetDatabase.GetAssetPath(bundleZip);
            binPath = Path.GetFullPath(Path.Combine(Application.dataPath, @".." + Path.DirectorySeparatorChar,
                binPath));
            var outStream = File.Create(binPath);
            var gzoStream = new GZipOutputStream(outStream);
            gzoStream.SetLevel(3);
            var tarOutputStream = new TarOutputStream(gzoStream);
            var tarCreator = new TarCreator(_engine.WorkingDir, _engine.WorkingDir) { IgnoreList = ignoreList };
            tarCreator.CreateTarFromIncludes(includes, tarOutputStream);

            Debug.Log($"bundle.tgz.bytes file updated. {tarOutputStream.Length} bytes {(DateTime.Now - t).TotalMilliseconds}ms");
            tarOutputStream.Close();
        }

        [ContextMenu("Zero Out bundle.tgz")]
        public void ZeroOutBundleZipWithPrompt() {
            if (bundleZip == null) {
                EditorUtility.DisplayDialog("bundle.tgz is null",
                    "Please make sure you have an bundle.tgz (Text Asset) set", "Okay");
                return;
            }
            if (EditorUtility.DisplayDialog("Are you sure?",
                    "This will zero out your bundle.tgz file. This is useful when you want to make a clean build.",
                    "Confirm", "Cancel")) {
                ZeroOutBundleZip();
            }
        }

        public void ZeroOutBundleZip() {
            var binPath = AssetDatabase.GetAssetPath(bundleZip);
            binPath = Path.GetFullPath(Path.Combine(Application.dataPath, @".." + Path.DirectorySeparatorChar,
                binPath));
            var outStream = File.Create(binPath);
            outStream.Close();
        }

#endif
    }

    [Serializable]
    public class DefaultFileMapping {
        public string path;
        public TextAsset textAsset;
    }
}