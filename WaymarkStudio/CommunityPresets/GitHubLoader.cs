using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WaymarkStudio;

internal class GitHubLoader
{
    internal static Task<PresetDirectory> Presets;
    static GitHubLoader()
    {
        Presets = LoadGitHubPresets();
    }

    internal static async Task<PresetDirectory> LoadGitHubPresets()
    {
        PresetDirectory rootDirectory = new();
        PresetDirectory currentDirectory = rootDirectory;
        HttpClient httpClient = new();
        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://github.com/sourpuh/ffxiv_waymarkstudio/wiki/community_presets.md"))
        {
            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            StreamReader reader = new(await response.Content.ReadAsStreamAsync());
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                if (line != null)
                {
                    if (line.StartsWith("#"))
                    {
                        currentDirectory = new();
                        currentDirectory.name = line.Substring(line.LastIndexOf("#")).Trim();
                        rootDirectory.children.Add(currentDirectory);
                    }
                    if (line.StartsWith("wms0"))
                    {
                        var preset = WaymarkPreset.Import(line);
                        preset.Time = DateTimeOffset.MinValue;
                        currentDirectory.presets.Add(preset);
                    }
                }
            }
        }
        Plugin.Storage.CommunityLibrary.InvalidateCache();
        return rootDirectory;
    }
}
