using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace WaymarkStudio.FFLogs;
internal class FFLogsClient
{
    private readonly HttpClient httpClient;

    internal FFLogsClient()
    {
        httpClient = new();
    }

    internal (string reportId, int fightIndex) ParseUrl(string fflogsUrl)
    {
        if (fflogsUrl == null || fflogsUrl.Length == 0)
            throw new ArgumentException("Invalid URI: No URI was provided.");
        var uri = new Uri(fflogsUrl);
        if (uri.Host != "www.fflogs.com")
            throw new ArgumentException("Invalid URI: The URI is not a valid FFLogs host.");
        if (!uri.AbsolutePath.StartsWith("/reports/"))
            throw new ArgumentException("Invalid URI: The URI is not an FFLogs report.");
        var reportId = uri.AbsolutePath.Substring("/reports/".Length);
        if (!Regex.IsMatch(reportId, "^[a-zA-Z0-9]+$"))
            throw new ArgumentException("Invalid URI: The URI has an invalid report ID.");

        var query = HttpUtility.ParseQueryString(uri.Query);
        var fightIndexStr = query.Get("fight");
        var fightIndex = -1;
        if (fightIndexStr != null)
            fightIndex = int.Parse(fightIndexStr) - 1;
        return (reportId, fightIndex);
    }

    internal async Task<List<Fight>> LoadFFLogsFights(string reportId)
    {
        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, $"https://www.fflogs.com/reports/fights-and-participants/{reportId}/0"))
        {
            requestMessage.Headers.Referrer = new($"https://www.fflogs.com/reports/{reportId}");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var fightsAndParticipants = await response.Content.ReadFromJsonAsync<FightsAndParticipants>();
            return fightsAndParticipants.Fights;
        }
    }

    internal async Task<WaymarkPreset> LoadFFLogsMarkers(string reportId, Fight fight)
    {
        uint boss = fight.Boss;
        uint startTime = fight.StartTime;
        uint territoryId = fight.ZoneId;

        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, $"https://www.fflogs.com/reports/replaysegment/{reportId}/{boss}/0/{startTime}"))
        {
            requestMessage.Headers.Referrer = new($"https://www.fflogs.com/reports/{reportId}");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var replaySegment = await response.Content.ReadFromJsonAsync<ReplaySegment>();
            return replaySegment.GetPreset(fight);
        }
    }

    public class Fight
    {
        public uint Id { get; set; }
        public uint Boss { get; set; }
        public uint ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        [JsonPropertyName("start_time")]
        public uint StartTime { get; set; }
    }
    public class FightsAndParticipants
    {
        public List<Fight>? Fights { get; set; }
    }

    public class WorldMarker
    {
        public int X { get; set; }
        public int Y { get; set; }
        public uint Icon { get; set; }
        public uint MapId { get; set; }

        public Waymark Waymark => (Waymark)Icon - 1;
        public Vector3 Position => new Vector3(X, 0, Y) / 100f;
    }
    public class Event
    {
        public string Type { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public uint Icon { get; set; }
        public uint MapId { get; set; }
        public int Timestamp { get; set; }

        public bool IsWorldMarkerPlaced => Type == "worldmarkerplaced";
        public bool IsWorldMarkerRemoved => Type == "worldmarkerremoved";

        public Waymark Waymark => (Waymark)Icon - 1;
        public Vector3 Position => new Vector3(X, 0, Y) / 100f;

        public WorldMarker WorldMarker => new WorldMarker()
        {
            X = X,
            Y = Y,
            Icon = Icon,
            MapId = MapId,
        };
    }
    public class ReplaySegment
    {
        public List<WorldMarker> WorldMarkers { get; set; } = new List<WorldMarker>();
        public List<Event> Events { get; set; } = new List<Event>();

        public WaymarkPreset GetPreset(Fight fight)
        {
            var territoryId = (ushort)fight.ZoneId;
            WaymarkPreset preset = new(
                name: $"FFLogs Fight {fight.Id}",
                territoryId: territoryId
            );

            foreach (var marker in WorldMarkers)
                preset.MarkerPositions[marker.Waymark] = marker.Position;
            foreach (var event_ in Events)
                if (event_.IsWorldMarkerPlaced)
                    preset.MarkerPositions[event_.Waymark] = event_.Position;
                else if (event_.IsWorldMarkerRemoved)
                    preset.MarkerPositions.Remove(event_.Waymark);
            preset.MarkPendingHeightAdjustment();

            return preset;
        }
    }
}
