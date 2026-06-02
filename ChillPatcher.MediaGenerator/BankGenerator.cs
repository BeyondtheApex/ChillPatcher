using System;
using System.IO;
using System.Text;

namespace ChillPatcher.MediaGenerator;

static class BankGenerator
{
    public static void Generate(ConfigModel config, string outputDir)
    {
        var bankDir = Path.Combine(config.GameDir, "media", "Audio", "FMODBanks");
        var stem = config.Bank.SourceBankStem;
        var metaFile = Path.Combine(bankDir, $"{stem}.bank");
        var assetFile = Path.Combine(bankDir, $"{stem}.assets.bank");

        if (!File.Exists(metaFile) || !File.Exists(assetFile))
        {
            Console.Error.WriteLine($"[bank] Source banks not found: {metaFile} / {assetFile}");
            return;
        }

        // 1) Copy .bank (master) as-is
        var outMeta = Path.Combine(outputDir, "media", "Audio", "FMODBanks", $"{stem}.bank");
        File.Copy(metaFile, outMeta, true);
        Console.WriteLine($"[bank] Copied  {stem}.bank  ({new FileInfo(metaFile).Length} bytes)");

        // 2) Parse the .assets.bank to understand its FSB5 structure
        var origBytes = File.ReadAllBytes(assetFile);
        var info = ParseAssetBank(origBytes);
        if (info == null)
        {
            Console.Error.WriteLine("[bank] Could not locate FSB5 inside SND chunk");
            return;
        }

        // 3) Rebuild with silent PCM
        var generated = Rebuild(info, config);
        var outAsset = Path.Combine(outputDir, "media", "Audio", "FMODBanks", $"{stem}.assets.bank");
        File.WriteAllBytes(outAsset, generated);
        Console.WriteLine($"[bank] Generated {stem}.assets.bank  ({generated.Length:N0} bytes)");
    }

    // ─── FSB5 parsing (self-contained, no internal deps) ───

    class Fsb5Info
    {
        public int Fsb5Offset;        // absolute offset of "FSB5" magic
        public uint Version;
        public uint NumSamples;
        public uint SizeOfSampleHeaders;
        public uint SizeOfNameTable;
        public uint SizeOfData;
        public uint Mode;
        public byte[] Hash = new byte[16];
        public uint Unknown;
        public uint HeaderSize;       // 0x3C or 0x40
        public int ExtraZerosAtStart; // padding before FSB5 inside SND (usually 16)
        public byte[] Skeleton;       // everything up to sample headers start
        public byte[] OrigSampleHeaders; // raw original sample headers
        public byte[] FullOriginal;   // full original file bytes (for full-header mode fallback)
    }

    static Fsb5Info? ParseAssetBank(byte[] data)
    {
        // Find "FSB5" signature
        int fsb5Off = IndexOf(data, new byte[] { 0x46, 0x53, 0x42, 0x35 }); // "FSB5"
        if (fsb5Off < 0) return null;

        int zerosBeforeFsb5 = 16;

        var info = new Fsb5Info { Fsb5Offset = fsb5Off, ExtraZerosAtStart = zerosBeforeFsb5, FullOriginal = data };

        using var ms = new MemoryStream(data, fsb5Off, data.Length - fsb5Off);
        using var r = new BinaryReader(ms);

        var magic = new string(r.ReadChars(4));
        if (magic != "FSB5") return null;

        info.Version = r.ReadUInt32();
        info.NumSamples = r.ReadUInt32();
        info.SizeOfSampleHeaders = r.ReadUInt32();
        info.SizeOfNameTable = r.ReadUInt32();
        info.SizeOfData = r.ReadUInt32();
        info.Mode = r.ReadUInt32();
        r.ReadUInt32(); // zero
        info.HeaderSize = info.Version == 0 ? 0x40u : 0x3Cu;

        if (info.Version == 0)
            r.ReadUInt32();

        r.ReadUInt32(); // flags
        info.Hash = r.ReadBytes(16);
        info.Unknown = r.ReadUInt32();

        int shdrStart = fsb5Off + (int)info.HeaderSize;

        // Skeleton = everything from start up to sample headers (not including headers)
        info.Skeleton = new byte[shdrStart];
        Array.Copy(data, 0, info.Skeleton, 0, shdrStart);

        // Original sample headers
        info.OrigSampleHeaders = new byte[info.SizeOfSampleHeaders];
        Array.Copy(data, shdrStart, info.OrigSampleHeaders, 0, info.OrigSampleHeaders.Length);

        LogFsb5(info);
        return info;
    }

    static void LogFsb5(Fsb5Info info)
    {
        Console.WriteLine($"[bank] FSB5: v{info.Version}, {info.NumSamples} samples, " +
                          $"sampleHdr={info.SizeOfSampleHeaders}, " +
                          $"dataSize={info.SizeOfData}, mode=0x{info.Mode:X8}, " +
                          $"headerSize=0x{info.HeaderSize:X}");
    }

    // ─── Rebuild ───

    const uint FSB5_MODE_PCM16 = 2;

    static byte[] Rebuild(Fsb5Info info, ConfigModel config)
    {
        int sampleRate = config.Bank.SampleRate;
        int bitsPerSample = config.Bank.BitsPerSample;
        int channels = config.Bank.Channels;
        int durationSec = config.Bank.SampleDurationSec;
        long totalSamples = (long)sampleRate * durationSec;
        int bytesPerSample = bitsPerSample / 8 * channels;
        long totalPcmBytes = totalSamples * bytesPerSample;

        Console.WriteLine($"[bank] Silent PCM: {totalSamples:N0} samples = {durationSec}s @ {sampleRate}Hz {bitsPerSample}bit x{channels}ch");

        long perSamplePcm = totalPcmBytes / info.NumSamples;
        Console.WriteLine($"[bank] Per sample: {perSamplePcm:N0} bytes");

        // Convert from original mode (may be Vorbis/full-headers) to PCM16 basic-headers
        bool convertToBasic = !IsBasicMode(info.SizeOfSampleHeaders, info.NumSamples);
        if (convertToBasic)
            Console.WriteLine("[bank] Converting from full-headers to basic-headers PCM16");

        // Build new FSB5 header (copy original, patch key fields)
        byte[] newHeader = new byte[info.HeaderSize];
        Array.Copy(info.Skeleton, info.Fsb5Offset, newHeader, 0, info.HeaderSize);
        WriteU32(newHeader, 0x0C, info.NumSamples * 8);    // SizeOfSampleHeaders = basic mode
        WriteU32(newHeader, 0x10, 0);                       // SizeOfNameTable = 0
        WriteU32(newHeader, 0x14, (uint)totalPcmBytes);     // SizeOfData
        WriteU32(newHeader, 0x18, FSB5_MODE_PCM16);         // Mode = PCM16

        // Build new sample headers (always basic mode, 8 bytes each)
        byte[] newSampleHdrs = BuildBasicSampleHeaders(info, (uint)perSamplePcm, sampleRate, channels);
        Console.WriteLine($"[bank] {info.NumSamples} basic headers x 8 bytes = {newSampleHdrs.Length} bytes");

        // Calculate result size
        // SND chunk layout: [4cc "SND "][4B size][16B zeros][FSB5 hdr][sample hdrs][PCM]
        int sndChunkOffset = info.Fsb5Offset - info.ExtraZerosAtStart - 8; // where "SND " fourCC is
        int prefixLen = sndChunkOffset; // bytes before SND chunk
        int newSndDataSize = info.ExtraZerosAtStart + newHeader.Length + newSampleHdrs.Length + (int)totalPcmBytes;
        int totalSize = prefixLen + 8 + newSndDataSize;

        byte[] result = new byte[totalSize];
        Array.Copy(info.Skeleton, 0, result, 0, prefixLen);

        // Write SND fourCC + size
        var sndBytes = new byte[] { 0x53, 0x4E, 0x44, 0x20 };
        Array.Copy(sndBytes, 0, result, prefixLen, 4);
        WriteU32(result, prefixLen + 4, (uint)newSndDataSize);

        // Update RIFF total size
        WriteU32(result, 4, (uint)(result.Length - 8));

        int pos = prefixLen + 8 + info.ExtraZerosAtStart;

        // FSB5 header
        Array.Copy(newHeader, 0, result, pos, newHeader.Length);
        pos += newHeader.Length;

        // Sample headers
        Array.Copy(newSampleHdrs, 0, result, pos, newSampleHdrs.Length);
        pos += newSampleHdrs.Length;

        // PCM is silent (zero-initialized)

        return result;
    }

    static bool IsBasicMode(uint sampleHeaderSize, uint numSamples)
        => sampleHeaderSize == numSamples * 8;

    static byte[] BuildBasicSampleHeaders(Fsb5Info info, uint perSampleBytes, int sampleRate, int channels)
    {
        // Map sample rate to FMOD frequency ID
        uint freqId = sampleRate switch
        {
            4000 => 0,
            8000 => 1,
            11000 => 2,
            12000 => 3,
            16000 => 4,
            22050 => 5,
            24000 => 6,
            32000 => 7,
            44100 => 8,
            48000 => 9,
            96000 => 10,
            _ => 8 // default 44100
        };

        // Map channels to FMOD channel bits
        uint chBits = channels switch { 1 => 0, 2 => 1, 6 => 2, 8 => 3, _ => 0 };

        byte[] result = new byte[info.NumSamples * 8];

        for (int i = 0; i < info.NumSamples; i++)
        {
            ulong byteOffset = (ulong)i * perSampleBytes;
            ulong dataOffsetUnits = byteOffset / 32;
            ulong sampleCount = perSampleBytes / 2; // 16-bit

            ulong packed = 0;
            // hasChunks = 0 (basic mode)
            packed |= (freqId << 1);
            packed |= (chBits << 5);
            packed |= (dataOffsetUnits << 7);
            packed |= (sampleCount << 34);

            WriteU64(result, i * 8, packed);
        }

        return result;
    }

    static int IndexOf(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
                if (data[i + j] != pattern[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    static void WriteU32(byte[] buf, int off, uint val) => BitConverter.GetBytes(val).CopyTo(buf, off);
    static void WriteU64(byte[] buf, int off, ulong val) => BitConverter.GetBytes(val).CopyTo(buf, off);
    static ulong ReadU64(byte[] buf, int off) => BitConverter.ToUInt64(buf, off);
}
