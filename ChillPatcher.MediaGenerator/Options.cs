namespace ChillPatcher.MediaGenerator;

class Options
{
    public string GameDir { get; set; } = "";
    public string OutputDir { get; set; } = "./media-generated";
    public string ConfigPath { get; set; } = "config.json";
    public bool SkipBanks { get; set; }
    public bool SkipXml { get; set; }
    public bool SkipAnthem { get; set; }
    public int? DurationSec { get; set; }

    /// <summary>PNG image to convert and inject into Anthem.zip</summary>
    public string? ReplaceImage { get; set; }

    /// <summary>Override target swatchbin path inside ZIP (default from config)</summary>
    public string? TargetSwatchBin { get; set; }
}
