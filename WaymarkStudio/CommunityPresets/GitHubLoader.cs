using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WaymarkStudio.Adapters;

namespace WaymarkStudio;

internal class GitHubLoader
{
    static string CommunityCacheFilePath => Plugin.Interface.ConfigDirectory + "/community_cache.md";
    internal static Task<PresetDirectory> Presets;
    static GitHubLoader()
    {
        Presets = InitialLoad();
    }

    internal static async Task<PresetDirectory> InitialLoad()
    {
        PresetDirectory rootDirectory = new();
        try
        {
            if (File.Exists(CommunityCacheFilePath))
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();
                rootDirectory = ReadCommunityPresets(new StreamReader(CommunityCacheFilePath));
                stopwatch.Stop();
                Plugin.Log.Info($"Loaded community presets from file in {stopwatch.Elapsed}");
                if (rootDirectory.presets.Count > 0)
                {
                    _ = RefreshPresetsFromGitHub();
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.ReportError(ex);
        }

        if (rootDirectory.presets.Count == 0)
        {
            try
            {
                rootDirectory = await LoadPresetsFromGitHub();
            }
            catch (Exception ex)
            {
                Plugin.ReportError(ex);
            }
        }

        Plugin.Storage.CommunityLibrary.InvalidateCache();
        return rootDirectory;
    }

    private static async Task RefreshPresetsFromGitHub()
    {
        var presets = LoadPresetsFromGitHub();
        await presets;
        Presets = presets;
        Plugin.Storage.CommunityLibrary.InvalidateCache();
    }

    private static async Task<PresetDirectory> LoadPresetsFromGitHub()
    {
        PresetDirectory rootDirectory = new();
        HttpClient httpClient = new();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://github.com/sourpuh/ffxiv_waymarkstudio/wiki/community_presets.md"))
        {
            var response = await httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                StreamReader reader = new(await response.Content.ReadAsStreamAsync());
                using (reader)
                    rootDirectory = ReadCommunityPresets(reader, true);
            }
            else
                Plugin.Log.Error($"Failed to load community presets from GitHub: {response.StatusCode}");
        }
        stopwatch.Stop();
        Plugin.Log.Info($"Loaded community presets from GitHub in {stopwatch.Elapsed}");
        return rootDirectory;
    }

    private static PresetDirectory ReadCommunityPresets(StreamReader reader, bool writeToFile = false)
    {
        StreamWriter writer = StreamWriter.Null;
        if (writeToFile)
            writer = new StreamWriter(CommunityCacheFilePath, false);

        PresetDirectory rootDirectory = new();
        PresetDirectory currentDirectory = rootDirectory;
        using (writer)
        {
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                writer.WriteLine(line);
                if (line.StartsWith("#"))
                {
                    currentDirectory = new();
                    currentDirectory.name = line.Substring(line.LastIndexOf("#")).Trim();
                    rootDirectory.children.Add(currentDirectory);
                }
                if (PresetImporter.IsTextImportable(line))
                {
                    var preset = PresetImporter.Import(line);
                    if (preset != null)
                    {
                        preset.Time = DateTimeOffset.MinValue;
                        currentDirectory.presets.Add(preset);
                    }
                }
            }
        }
        return rootDirectory;
    }
}
