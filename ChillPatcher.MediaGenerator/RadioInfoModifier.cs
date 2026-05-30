using System;
using System.IO;
using System.Xml;

namespace ChillPatcher.MediaGenerator;

static class RadioInfoModifier
{
    public static void Modify(ConfigModel config, string outputDir)
    {
        var audioDir = Path.Combine(config.GameDir, "media", "Audio");
        var outAudioDir = Path.Combine(outputDir, "Audio");

        var locales = new[] { "BR", "CN", "DE", "EN", "ES", "IT", "JP", "KO", "MX", "TW" };

        foreach (var locale in locales)
        {
            var srcFile = Path.Combine(audioDir, $"RadioInfo_{locale}.xml");
            if (!File.Exists(srcFile))
            {
                Console.Error.WriteLine($"[xml] Source not found: {srcFile}");
                continue;
            }

            var xml = new XmlDocument();
            xml.Load(srcFile);

            var nsMgr = new XmlNamespaceManager(xml.NameTable);
            // No namespaces in these files, but add for safety
            var root = xml.DocumentElement!;

            // Find the target station by Name
            var stations = root.SelectNodes("RadioStations/RadioStation");
            if (stations == null) continue;

            XmlElement? targetStation = null;
            foreach (XmlElement station in stations)
            {
                var nameAttr = station.GetAttribute("Name");
                if (nameAttr == config.RadioInfo.StationName)
                {
                    targetStation = station;
                    break;
                }
            }

            if (targetStation == null)
            {
                Console.Error.WriteLine($"[xml] Station '{config.RadioInfo.StationName}' not found in RadioInfo_{locale}.xml");
                continue;
            }

            // Modify the Track SampleList
            var trackList = targetStation.SelectSingleNode("SampleList[@Type='Track']") as XmlElement;
            if (trackList == null) continue;

            // Remove all existing samples
            while (trackList.HasChildNodes)
                trackList.RemoveChild(trackList.FirstChild!);

            // Add our single track sample
            int totalSamples = config.Bank.SampleRate * config.Bank.SampleDurationSec;
            var sample = xml.CreateElement("Sample");
            sample.SetAttribute("SoundName", config.RadioInfo.TrackSampleName);
            sample.SetAttribute("SampleLength", totalSamples.ToString());
            sample.SetAttribute("SampleRate", config.Bank.SampleRate.ToString());
            sample.SetAttribute("DisplayName", config.RadioInfo.DisplayName);
            sample.SetAttribute("Artist", config.RadioInfo.Artist);
            sample.SetAttribute("IsXCloudModeSafe", "true");

            // Add minimal markers (required by game)
            AddMarker(xml, sample, "VeryStart", "-1");
            AddMarker(xml, sample, "TrackStart", "0");
            AddMarker(xml, sample, "DJDrop", "0");
            AddMarker(xml, sample, "TrackDrop", "0");
            AddMarker(xml, sample, "TrackLoopStart", "0");
            AddMarker(xml, sample, "TrackLoopEnd", totalSamples.ToString());
            AddMarker(xml, sample, "DJSegment", "-1");
            AddMarker(xml, sample, "PostDrop", "-1");
            AddMarker(xml, sample, "PostRaceLoopStart", "0");
            AddMarker(xml, sample, "PostRaceLoopEnd", "-1");
            AddMarker(xml, sample, "StingerStart", totalSamples.ToString());
            AddMarker(xml, sample, "DJStart", (totalSamples + 1000).ToString());
            AddMarker(xml, sample, "End", totalSamples.ToString());
            for (int j = 1; j <= 5; j++) { AddMarker(xml, sample, $"Loop{j}Start", "-1"); AddMarker(xml, sample, $"Loop{j}End", "-1"); }
            for (int j = 1; j <= 5; j++) AddMarker(xml, sample, $"Section{j}", "-1");
            AddMarker(xml, sample, "BinkTransition", "-1");

            // Loops
            AddLoop(xml, sample, "TrackMain", "TrackLoopStart", "TrackLoopEnd");
            AddLoop(xml, sample, "TrackPostRace", "PostRaceLoopStart", "PostRaceLoopEnd");
            for (int j = 1; j <= 5; j++) AddLoop(xml, sample, $"Loop{j}", $"Loop{j}Start", $"Loop{j}End");

            // BPM
            var bpm = xml.CreateElement("BPM");
            bpm.SetAttribute("Value", "120.0");
            bpm.SetAttribute("Start", "0");
            sample.AppendChild(bpm);

            trackList.AppendChild(sample);

            // Clear DJ, Stinger, StingerLFE lists (no DJ chatter for custom station)
            ClearSampleList(targetStation, "DJ");
            ClearSampleList(targetStation, "Stinger");
            ClearSampleList(targetStation, "TrackLFE");

            // Preserve original PlayLists — mod's DSP handles playback at runtime,
            // but the game UI needs the PlayList entries for "Now Playing" display.
            // fh6-universal-radio keeps them intact, so do we.
            Console.WriteLine($"[xml]  PlayLists preserved as-is for {locale}");

            var outFile = Path.Combine(outAudioDir, $"RadioInfo_{locale}.xml");
            xml.Save(outFile);
            Console.WriteLine($"[xml]  Modified  RadioInfo_{locale}.xml");
        }
    }

    static void AddMarker(XmlDocument doc, XmlElement parent, string name, string position)
    {
        var m = doc.CreateElement("Marker");
        m.SetAttribute("Name", name);
        m.SetAttribute("Position", position);
        parent.AppendChild(m);
    }

    static void AddLoop(XmlDocument doc, XmlElement parent, string name, string startMarker, string endMarker)
    {
        var l = doc.CreateElement("Loop");
        l.SetAttribute("Name", name);
        l.SetAttribute("StartMarker", startMarker);
        l.SetAttribute("EndMarker", endMarker);
        parent.AppendChild(l);
    }

    static void ClearSampleList(XmlElement station, string type)
    {
        var list = station.SelectSingleNode($"SampleList[@Type='{type}']") as XmlElement;
        if (list != null)
            while (list.HasChildNodes) list.RemoveChild(list.FirstChild!);
    }
}
