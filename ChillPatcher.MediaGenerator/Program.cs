using System;
using System.IO;
using ChillPatcher.MediaGenerator;

var options = ParseArgs(args);
if (options == null) return 1;

var config = ConfigLoader.Load(options.ConfigPath);
config.ApplyOverrides(options);

Console.WriteLine($"[chill-gen-media] Game dir : {config.GameDir}");
Console.WriteLine($"[chill-gen-media] Output   : {options.OutputDir}");
Console.WriteLine($"[chill-gen-media] Duration : {config.Bank.SampleDurationSec}s");
Console.WriteLine();

Directory.CreateDirectory(options.OutputDir);
Directory.CreateDirectory(Path.Combine(options.OutputDir, "media", "Audio", "FMODBanks"));
Directory.CreateDirectory(Path.Combine(options.OutputDir, "media", "UI", "Textures"));
Directory.CreateDirectory(Path.Combine(options.OutputDir, "media", "UI", "Textures", "HiRes"));

if (!options.SkipBanks)
    BankGenerator.Generate(config, options.OutputDir);

if (!options.SkipXml)
    RadioInfoModifier.Modify(config, options.OutputDir);

if (!options.SkipAnthem)
    AnthemHandler.Process(config, options.OutputDir, options.ReplaceImage);

Console.WriteLine("\nDone.");
return 0;


static Options? ParseArgs(string[] args)
{
    var opt = new Options();
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-g" or "--game-dir" when i + 1 < args.Length:
                opt.GameDir = args[++i]; break;
            case "-o" or "--output-dir" when i + 1 < args.Length:
                opt.OutputDir = args[++i]; break;
            case "-c" or "--config" when i + 1 < args.Length:
                opt.ConfigPath = args[++i]; break;
            case "-d" or "--duration-sec" when i + 1 < args.Length:
                opt.DurationSec = int.Parse(args[++i]); break;
            case "--skip-banks": opt.SkipBanks = true; break;
            case "--skip-xml": opt.SkipXml = true; break;
            case "--skip-anthem": opt.SkipAnthem = true; break;
            case "-r" or "--replace-image" when i + 1 < args.Length:
                opt.ReplaceImage = args[++i]; break;
            case "-t" or "--target" when i + 1 < args.Length:
                opt.TargetSwatchBin = args[++i]; break;
            case "-h" or "--help":
                PrintHelp(); return null;
        }
    }
    if (string.IsNullOrEmpty(opt.GameDir))
    {
        Console.Error.WriteLine("Error: --game-dir is required.");
        PrintHelp();
        return null;
    }
    return opt;
}

static void PrintHelp()
{
    Console.WriteLine(@"
FH6 Radio Media Overlay Generator
=================================
Usage: chill-gen-media [options]

Options:
  -g, --game-dir <path>    FH6 install directory (required)
  -o, --output-dir <path>  Output directory (default: ./media-generated)
  -c, --config <path>      JSON config file (default: config.json)
  -d, --duration-sec <n>   Override sample duration in seconds
  --skip-banks             Skip FMOD bank generation
  --skip-xml               Skip RadioInfo XML modification
  --skip-anthem            Skip Anthem.zip processing
  -r, --replace-image <png> PNG image to convert to swatchbin and inject
  -t, --target <path>       Override target path inside ZIP (default from config)
  -h, --help               Show this help
");
}
