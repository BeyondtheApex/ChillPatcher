using System;
using System.IO;
using Newtonsoft.Json;

namespace ChillPatcher.MediaGenerator;

class ConfigModel
{
    public string GameDir { get; set; } = "";
    public BankConfig Bank { get; set; } = new();
    public RadioInfoConfig RadioInfo { get; set; } = new();
    public AnthemConfig AnthemZip { get; set; } = new();
}

class BankConfig
{
    public string SourceBankStem { get; set; } = "R9_Tracks_CU1";
    public int SampleDurationSec { get; set; } = 300;
    public int SampleRate { get; set; } = 44100;
    public int BitsPerSample { get; set; } = 16;
    public int Channels { get; set; } = 1;
}

class RadioInfoConfig
{
    public int StationNumber { get; set; } = 10;
    public string StationName { get; set; } = "Streamer Mode";
    public string TrackSampleName { get; set; } = "HZ6_R9_PeterBroderick_EyesClosedandTraveling";
    public string DisplayName { get; set; } = "OmniMix Player";
    public string Artist { get; set; } = "ChillPatcher";
}

class AnthemConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>Path inside Anthem.zip to copy logo from (e.g. "HUD/RadioLogos/Horizon_Pulse.swatchbin")</summary>
    public string CopyLogoFrom { get; set; } = "HUD/RadioLogos/Horizon_Pulse.swatchbin";

    /// <summary>Path inside Anthem.zip to replace (e.g. "HUD/RadioLogos/Streamer_Mode.swatchbin")</summary>
    public string TargetSwatchBin { get; set; } = "HUD/RadioLogos/Streamer_Mode.swatchbin";

    /// <summary>"partial" = only include non-empty entries (strip empty dirs, mod-style);
    /// "full" = keep all original entries including empty dirs.</summary>
    public string Mode { get; set; } = "partial";
}

static class ConfigLoader
{
    public static ConfigModel Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config not found: {path}");

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<ConfigModel>(json)
               ?? throw new InvalidOperationException("Failed to parse config");
    }
}

static class ConfigModelExtensions
{
    public static void ApplyOverrides(this ConfigModel config, Options options)
    {
        if (!string.IsNullOrEmpty(options.GameDir))
            config.GameDir = options.GameDir;
        if (options.DurationSec.HasValue)
            config.Bank.SampleDurationSec = options.DurationSec.Value;
        if (!string.IsNullOrEmpty(options.TargetSwatchBin))
            config.AnthemZip.TargetSwatchBin = options.TargetSwatchBin;
    }
}
