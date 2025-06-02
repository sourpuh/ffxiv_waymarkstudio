using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WaymarkStudio;

internal class GitHubLoader
{
    internal static Task<List<WaymarkPreset>> Presets;
    static GitHubLoader()
    {
        Presets = LoadGitHubPresets();
    }

    internal static async Task<List<WaymarkPreset>> LoadGitHubPresets()
    {
        List<WaymarkPreset> presets = new();
        HttpClient httpClient = new();
        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://github.com/sourpuh/ffxiv_waymarkstudio/wiki/community_presets.md"))
        {
            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            StreamReader reader = new(await response.Content.ReadAsStreamAsync());
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                if (line != null && line.StartsWith("wms0"))
                {
                    var preset = WaymarkPreset.Import(line);
                    preset.Time = DateTimeOffset.MinValue;
                    presets.Add(preset);
                }
            }
        }
        Plugin.Storage.CommunityLibrary.InvalidateCache();
        return presets;
    }
}
