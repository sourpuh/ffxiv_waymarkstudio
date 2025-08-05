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
            if (await Task.Run(() => File.Exists(CommunityCacheFilePath)))
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();
                await using var fileStream = new FileStream(
                    CommunityCacheFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);
                using var streamReader = new StreamReader(fileStream);
                rootDirectory = await ReadCommunityPresets(streamReader);
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
        // Delay in case the file handle is still in use by the initial loader.
        await Task.Delay(1000);
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
            using var response = await httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                StreamReader reader = new(await response.Content.ReadAsStreamAsync());
                using (reader)
                    rootDirectory = await ReadCommunityPresets(reader, true);
            }
            else
                Plugin.Log.Error($"Failed to load community presets from GitHub: {response.StatusCode}");
        }
        stopwatch.Stop();
        Plugin.Log.Info($"Loaded community presets from GitHub in {stopwatch.Elapsed}");
        return rootDirectory;
    }

    private static async Task<PresetDirectory> ReadCommunityPresets(StreamReader reader, bool writeToFile = false)
    {
        StreamWriter writer = StreamWriter.Null;
        if (writeToFile)
            writer = new StreamWriter(CommunityCacheFilePath, append: false);

        PresetDirectory rootDirectory = new();
        PresetDirectory currentDirectory = rootDirectory;
        using (writer)
        {
            while (reader.Peek() != -1)
            {
                var line = await reader.ReadLineAsync() ?? "";
                writer.WriteLine(line);
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
