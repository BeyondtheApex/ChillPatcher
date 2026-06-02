using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ChillPatcher.MediaGenerator;

static class AnthemHandler
{
    public static void Process(ConfigModel config, string outputDir, string? replaceImage = null)
    {
        if (!config.AnthemZip.Enabled)
        {
            Console.WriteLine("[anthem] Skipped (disabled in config)");
            return;
        }

        var srcLogo = config.AnthemZip.CopyLogoFrom;
        var targetLogo = config.AnthemZip.TargetSwatchBin;
        var mode = config.AnthemZip.Mode?.ToLowerInvariant() ?? "partial";
        var useImage = !string.IsNullOrEmpty(replaceImage);

        if (string.IsNullOrEmpty(targetLogo) || (!useImage && string.IsNullOrEmpty(srcLogo)))
        {
            Console.Error.WriteLine("[anthem] targetSwatchBin (and copyLogoFrom for copy mode) must be set");
            return;
        }

        var uiDir = Path.Combine(config.GameDir, "media", "UI", "Textures");
        var outUiDir = Path.Combine(outputDir, "media", "UI", "Textures");

        // Process both normal and HiRes
        ProcessAnthemZip(
            Path.Combine(uiDir, "Anthem.zip"),
            Path.Combine(outUiDir, "Anthem.zip"),
            srcLogo, targetLogo, mode, replaceImage);

        ProcessAnthemZip(
            Path.Combine(uiDir, "HiRes", "Anthem.zip"),
            Path.Combine(outUiDir, "HiRes", "Anthem.zip"),
            srcLogo, targetLogo, mode, replaceImage);
    }

    static void ProcessAnthemZip(string srcPath, string dstPath,
        string copyLogoFrom, string targetSwatchBin, string mode, string? replaceImage = null)
    {
        if (!File.Exists(srcPath))
        {
            Console.Error.WriteLine($"[anthem] Source not found: {srcPath}");
            return;
        }

        // Read entire source ZIP into memory (entries can only be read while archive is open)
        byte[]? sourceSwatchbinBytes = null;
        bool useImageConversion = !string.IsNullOrEmpty(replaceImage);
        var allEntries = new List<ZipEntryData>();

        using (var srcZip = ZipFile.OpenRead(srcPath))
        {
            if (useImageConversion)
            {
                // PNG→SwatchBin conversion mode:
                // Read the target swatchbin to get its header/format, then build from PNG
                var targetEntry = srcZip.GetEntry(NormalizeZipPath(targetSwatchBin));
                if (targetEntry == null)
                {
                    Console.Error.WriteLine($"[anthem] Target not found in ZIP for format reference: {targetSwatchBin}");
                    return;
                }
                byte[] targetBytes;
                using (var ts = targetEntry.Open())
                    targetBytes = ReadAllBytes(ts, (int)targetEntry.Length);

                var info = SwatchBinBuilder.Parse(targetBytes);
                Console.WriteLine($"[anthem] Converting '{replaceImage}' → swatchbin ({info.Width}x{info.Height}, {info.Format})");
                sourceSwatchbinBytes = SwatchBinBuilder.BuildFromPng(replaceImage!, info);
            }
            else
            {
                // Copy mode: read source logo swatchbin
                var srcEntry = srcZip.GetEntry(NormalizeZipPath(copyLogoFrom));
                if (srcEntry == null)
                {
                    Console.Error.WriteLine($"[anthem] Source logo not found in ZIP: {copyLogoFrom}");
                    return;
                }
                using (var s = srcEntry.Open())
                    sourceSwatchbinBytes = ReadAllBytes(s, (int)srcEntry.Length);
                Console.WriteLine($"[anthem] Read source logo '{copyLogoFrom}' ({sourceSwatchbinBytes.Length:N0} bytes)");
            }

            // Read all entries
            bool foundTarget = false;
            foreach (var entry in srcZip.Entries)
            {
                var normalized = NormalizeZipPath(entry.FullName);

                if (normalized == NormalizeZipPath(targetSwatchBin))
                {
                    foundTarget = true;
                    allEntries.Add(new ZipEntryData
                    {
                        Path = entry.FullName,
                        Data = sourceSwatchbinBytes,
                        IsDirectory = false
                    });
                    if (useImageConversion)
                        Console.WriteLine($"[anthem] Replaced '{targetSwatchBin}' with converted image ({sourceSwatchbinBytes.Length:N0} bytes)");
                    else
                        Console.WriteLine($"[anthem] Replaced '{targetSwatchBin}' with '{copyLogoFrom}' ({sourceSwatchbinBytes.Length:N0} bytes)");
                }
                else if (IsEmptyDirectory(entry))
                {
                    if (mode == "full")
                        allEntries.Add(new ZipEntryData { Path = entry.FullName, Data = null, IsDirectory = true });
                }
                else
                {
                    using (var s = entry.Open())
                    {
                        allEntries.Add(new ZipEntryData
                        {
                            Path = entry.FullName,
                            Data = ReadAllBytes(s, (int)entry.Length),
                            IsDirectory = false
                        });
                    }
                }
            }

            if (!foundTarget)
                Console.Error.WriteLine($"[anthem] WARNING: target '{targetSwatchBin}' not found in ZIP — adding as new entry");
        }

        if (sourceSwatchbinBytes == null)
        {
            Console.Error.WriteLine("[anthem] No replacement data generated");
            return;
        }

        // Write modified ZIP
        Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
        if (File.Exists(dstPath))
            File.Delete(dstPath);
        using (var dstZip = ZipFile.Open(dstPath, ZipArchiveMode.Create))
        {
            foreach (var entry in allEntries)
            {
                if (entry.IsDirectory)
                {
                    // Create empty directory entry
                    var ze = dstZip.CreateEntry(entry.Path);
                    // No data to write
                }
                else
                {
                    var ze = dstZip.CreateEntry(entry.Path, CompressionLevel.Optimal);
                    if (entry.Data != null)
                    {
                        using (var s = ze.Open())
                            s.Write(entry.Data, 0, entry.Data.Length);
                    }
                }
            }
        }

        var fi = new FileInfo(dstPath);
        Console.WriteLine($"[anthem] Written {Path.GetFileName(dstPath)} ({fi.Length:N0} bytes, {allEntries.Count} entries, mode={mode})");
    }

    struct ZipEntryData
    {
        public string Path;
        public byte[]? Data;
        public bool IsDirectory;
    }

    static bool IsEmptyDirectory(ZipArchiveEntry entry)
        => string.IsNullOrEmpty(entry.Name) || entry.Length == 0 && entry.Name.EndsWith("/");

    static string NormalizeZipPath(string path) => path.Replace('\\', '/').Trim('/');

    static byte[] ReadAllBytes(Stream stream, int length)
    {
        var buf = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = stream.Read(buf, offset, length - offset);
            if (read == 0) break;
            offset += read;
        }
        return buf;
    }
}
