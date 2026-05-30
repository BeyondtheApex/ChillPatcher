using System;
using System.IO;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SkiaSharp;

namespace ChillPatcher.MediaGenerator;

/// <summary>
/// Parses and generates FH6 SwatchBin texture files.
/// Format: burG container → BCXT chunk → [HCXT + mip table] → BC7 pixel data.
/// </summary>
static class SwatchBinBuilder
{
    // ─── Header parsing ───

    public readonly struct SwatchBinInfo
    {
        public readonly int HeaderSize;       // burG[0x08] — offset where pixel data starts
        public readonly int TotalSize;         // burG[0x0C] — total file size
        public readonly int DataSize;          // size of pixel data region
        public readonly int Width;             // decoded from HCXT
        public readonly int Height;            // decoded from HCXT
        public readonly CompressionFormat Format;
        public readonly byte[] HeaderBytes;    // everything before pixel data

        public SwatchBinInfo(int headerSize, int totalSize, int width, int height,
            CompressionFormat format, byte[] headerBytes)
        {
            HeaderSize = headerSize;
            TotalSize = totalSize;
            DataSize = totalSize - headerSize;
            Width = width;
            Height = height;
            Format = format;
            HeaderBytes = headerBytes;
        }
    }

    /// <summary>Parse a swatchbin to extract dimensions and header.</summary>
    public static SwatchBinInfo Parse(byte[] data)
    {
        if (data.Length < 0x60)
            throw new InvalidDataException("SwatchBin too small");

        // burG header
        var magic = System.Text.Encoding.ASCII.GetString(data, 0, 4);
        if (magic != "burG")
            throw new InvalidDataException($"Not a swatchbin: bad magic '{magic}'");

        int headerSize = BitConverter.ToInt32(data, 0x08);
        int totalSize = BitConverter.ToInt32(data, 0x0C);

        // BCXT header
        var bcxtMagic = System.Text.Encoding.ASCII.GetString(data, 0x14, 4);
        if (bcxtMagic != "BCXT")
            throw new InvalidDataException("Missing BCXT chunk");

        // HCXT header (at 0x2C)
        var hcxtMagic = System.Text.Encoding.ASCII.GetString(data, 0x2C, 4);
        if (hcxtMagic != "HCXT")
            throw new InvalidDataException("Missing HCXT chunk");

        // Width/Height: stored after HCXT sub-header.
        // For radio logos: at offset 0x4C (width) and 0x50 (height)
        int width = BitConverter.ToInt32(data, 0x4C);
        int height = BitConverter.ToInt32(data, 0x50);

        // Detect format from HCXT flags (byte at 0x5B: 0x06=BC7, 0x01=BC1)
        CompressionFormat format = data[0x5B] switch
        {
            0x06 => CompressionFormat.Bc7,
            0x01 => CompressionFormat.Bc1,
            _ => CompressionFormat.Bc7  // default for radio logos
        };

        var headerBytes = new byte[headerSize];
        Array.Copy(data, 0, headerBytes, 0, headerSize);

        return new SwatchBinInfo(headerSize, totalSize, width, height, format, headerBytes);
    }

    // ─── Build from PNG ───

    /// <summary>
    /// Generate a swatchbin from a PNG, matching the format of an existing target.
    /// PNG is resized to match target dimensions.
    /// </summary>
    public static byte[] BuildFromPng(string pngPath, SwatchBinInfo targetInfo)
    {
        if (!File.Exists(pngPath))
            throw new FileNotFoundException($"PNG not found: {pngPath}");

        // Load PNG with SkiaSharp
        using var skBitmap = SKBitmap.Decode(pngPath);
        if (skBitmap == null)
            throw new InvalidDataException($"Failed to decode PNG: {pngPath}");

        int tw = targetInfo.Width;
        int th = targetInfo.Height;

        Console.WriteLine($"[swb] PNG loaded: {skBitmap.Width}x{skBitmap.Height} → resizing to {tw}x{th}");

        // Resize with SkiaSharp 3.x API (SKSamplingOptions instead of deprecated SKFilterQuality)
        using var resized = skBitmap.Resize(new SKSizeI(tw, th), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (resized == null)
            throw new InvalidOperationException("Resize failed");

        // Extract RGBA bytes
        var rgba = new byte[tw * th * 4];
        var ptr = resized.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgba, 0, rgba.Length);

        Console.WriteLine($"[swb] Extracted {rgba.Length:N0} RGBA bytes, compressing to {targetInfo.Format}...");

        // Compress — BCnEncoder.Net API: EncodeToRawBytes(Span<byte>, width, height, PixelFormat) returns byte[][]
        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                Quality = CompressionQuality.Fast,
                Format = targetInfo.Format,
                GenerateMipMaps = true
            }
        };

        var allMips = encoder.EncodeToRawBytes(rgba.AsSpan(), tw, th, PixelFormat.Rgba32);

        // Flatten all mip levels into single array
        long totalLen = 0;
        foreach (var mip in allMips)
            totalLen += mip.Length;

        var compressed = new byte[totalLen];
        long offset = 0;
        foreach (var mip in allMips)
        {
            Array.Copy(mip, 0, compressed, offset, mip.Length);
            offset += mip.Length;
        }

        Console.WriteLine($"[swb] BC7: {allMips.Length} mip levels, {totalLen:N0} bytes total");

        // Build swatchbin: header + compressed data
        int newTotalSize = targetInfo.HeaderSize + compressed.Length;
        var result = new byte[newTotalSize];

        // Copy header
        Array.Copy(targetInfo.HeaderBytes, 0, result, 0, targetInfo.HeaderSize);

        // Update size fields
        WriteU32(result, 0x0C, (uint)newTotalSize);              // burG total size
        WriteU32(result, 0x24, (uint)compressed.Length);          // BCXT dataSize
        WriteU32(result, 0x28, (uint)compressed.Length);          // BCXT dataSizeDup

        // Update mip table top-level entry
        UpdateMipTableTopLevel(result, targetInfo.HeaderSize, compressed.Length);

        // Copy compressed pixel data
        Array.Copy(compressed, 0, result, targetInfo.HeaderSize, compressed.Length);

        Console.WriteLine($"[swb] Built swatchbin: {newTotalSize:N0} bytes " +
                          $"({targetInfo.HeaderSize} header + {compressed.Length} BCn data)");

        return result;
    }

    /// <summary>
    /// Updates the top-level mip data size entry in the mip table.
    /// </summary>
    static void UpdateMipTableTopLevel(byte[] result, int headerSize, int newDataSize)
    {
        // Scan backwards from headerSize for the 0xFFFFFFFF terminator
        for (int i = headerSize - 4; i >= 0x64; i -= 4)
        {
            if (BitConverter.ToUInt32(result, i) == 0xFFFFFFFF)
            {
                WriteU32(result, i - 4, (uint)newDataSize);
                return;
            }
        }
    }

    static void WriteU32(byte[] buf, int off, uint val) => BitConverter.GetBytes(val).CopyTo(buf, off);
}
