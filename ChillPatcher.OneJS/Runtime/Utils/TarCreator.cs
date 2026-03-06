using System;
using System.Collections.Generic;
using System.IO;
using DotNet.Globbing;
using ICSharpCode.SharpZipLib.Tar;
using NUglify;
using OneJS.Compat;
using UnityEngine;

namespace OneJS.Utils {
    public class TarCreator {
        public bool ExcludeTS { get; set; }
        public bool ExcludeTSDef { get; set; }
        public bool UglifyJS { get; set; }
        public string[] IgnoreList { get; set; }
        public bool IncludeRoot { get; set; } = true;

        string _baseDir;
        string _rootDir;

        /**
         * Creates a new TarCreator.
         * @param baseDir The base directory to start from.
         * @param rootDir The root directory. i.e. ScriptEngine WorkingDir
         */
        public TarCreator(string baseDir, string rootDir) {
            _baseDir = baseDir;
            _rootDir = rootDir;
        }

        /// <summary>
        /// Creates a single tar file from the given top-level includes. Basically, it calls CreateTar() for each include.
        /// </summary>
        public void CreateTarFromIncludes(string[] includes, TarOutputStream tarOutputStream) {
            foreach (var include in includes) {
                var includePath = Path.Combine(_baseDir, include);
                if (Directory.Exists(includePath)) {
                    CreateTar(tarOutputStream, includePath);
                } else if (File.Exists(includePath)) {
                    WriteEntry(tarOutputStream, includePath, include);
                } else {
                    Debug.Log($"Include not found: {includePath}");
                }
            }
        }

        public void CreateTar(TarOutputStream tarOutputStream, string curDir = null) {
            curDir ??= _baseDir;
            // var baseBaseDir = Path.GetFullPath(IncludeRoot ? Path.Combine(_baseDir, "../") : _baseDir);
            var baseBaseDir = Path.GetFullPath(IncludeRoot ? _rootDir : _baseDir);

            string[] filenames = Directory.GetFiles(curDir);
            foreach (string filepath in filenames) {
                string tarName = NetFxCompat.GetRelativePath(baseBaseDir, filepath);
                WriteEntry(tarOutputStream, filepath, tarName);
            }

            var di = new DirectoryInfo(curDir);
            foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
                if (dir.Name != ".git" && !IsIgnored(dir.FullName))
                    CreateTar(tarOutputStream, dir.FullName);
            }
        }

        public void WriteEntry(TarOutputStream tarOutputStream, string filepath, string tarName) {
            if (ExcludeTS && ((filepath.EndsWith(".ts") && !filepath.EndsWith(".d.ts")) || filepath.EndsWith(".tsx")))
                return;
            if (ExcludeTSDef && filepath.EndsWith(".d.ts"))
                return;
            if (IsIgnored(filepath))
                return;
            byte[] bytes;
            if (UglifyJS && filepath.EndsWith(".js")) {
                var str = File.ReadAllText(filepath);
                try {
                    var res = Uglify.Js(str);
                    if (res.HasErrors) {
                        Debug.Log($"Could not uglify {filepath}\n\n" + string.Join("\n\n", res.Errors));
                        return;
                    }
                    bytes = System.Text.Encoding.UTF8.GetBytes(res.Code);
                } catch (Exception e) {
                    Debug.Log($"Could not uglify {filepath}\n\n" + e.Message);
                    return;
                }
            } else {
                bytes = File.ReadAllBytes(filepath);
            }
            TarEntry entry = TarEntry.CreateTarEntry(tarName.Replace(@"\", @"/"));
            entry.Size = bytes.Length;
            tarOutputStream.PutNextEntry(entry);
            tarOutputStream.Write(bytes, 0, bytes.Length);
            tarOutputStream.CloseEntry();
        }

        bool IsIgnored(string filepath) {
            if (IgnoreList == null || IgnoreList.Length == 0)
                return false;
            var path = NetFxCompat.GetRelativePath(_rootDir, filepath);
            var pttrns = new List<string>(IgnoreList);
            pttrns.Insert(0, "**/.git*");
            foreach (var pttrn in pttrns) {
                var glob = Glob.Parse(pttrn);
                var isMatch = glob.IsMatch(path);
                if (isMatch) {
                    Debug.Log($"IGNORED: {filepath}");
                    return true;
                }
            }
            return false;
        }

        public static void CreateTarManually(TarOutputStream tarOutputStream, string baseDir, string curDir = null) {
            curDir ??= baseDir;
            var baseBaseDir = Path.GetFullPath(Path.Combine(baseDir, "../"));
            // Optionally, write an entry for the directory itself.
            // TarEntry tarEntry = TarEntry.CreateEntryFromFile(curDir);
            // tarOutputStream.PutNextEntry(tarEntry);

            // Write each file to the tar.
            string[] filenames = Directory.GetFiles(curDir);
            foreach (string filepath in filenames) {
                string tarName = NetFxCompat.GetRelativePath(baseBaseDir, filepath);
                WriteFileEntry(tarOutputStream, filepath, tarName);
            }

            var di = new DirectoryInfo(curDir);
            foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
                if (dir.Name != ".git")
                    CreateTarManually(tarOutputStream, baseDir, dir.FullName);
            }
        }

        public static void WriteFileEntry(TarOutputStream tarOutputStream, string filepath, string tarName) {
            if ((filepath.EndsWith(".ts") && !filepath.EndsWith(".d.ts")) || filepath.EndsWith(".tsx"))
                return;
            var str = File.ReadAllText(filepath);
            if (filepath.EndsWith(".js")) {
                var res = Uglify.Js(str);
                if (res.HasErrors) {
                    Debug.Log($"Could not uglify {filepath}\n\n" + string.Join("\n\n", res.Errors));
                    return;
                }
                str = res.Code;
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            // using (Stream inputStream = File.OpenRead(filepath)) {
            // long fileSize = inputStream.Length;

            // Create a tar entry named as appropriate. You can set the name to anything,
            // but avoid names starting with drive or UNC.
            TarEntry entry = TarEntry.CreateTarEntry(tarName.Replace(@"\", @"/"));

            // Must set size, otherwise TarOutputStream will fail when output exceeds.
            entry.Size = bytes.Length;

            // Add the entry to the tar stream, before writing the data.
            tarOutputStream.PutNextEntry(entry);
            tarOutputStream.Write(bytes, 0, bytes.Length);

            // this is copied from TarArchive.WriteEntryCore
            // byte[] localBuffer = new byte[32 * 1024];
            // while (true) {
            //     int numRead = inputStream.Read(localBuffer, 0, localBuffer.Length);
            //     if (numRead <= 0)
            //         break;
            //
            //     tarOutputStream.Write(localBuffer, 0, numRead);
            // }
            // }
            tarOutputStream.CloseEntry();
        }
    }
}