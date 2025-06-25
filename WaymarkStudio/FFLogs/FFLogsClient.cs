using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WaymarkStudio.FFLogs;
internal class FFLogsClient
{
    private readonly HttpClient httpClient;

    internal FFLogsClient()
    {
        httpClient = new();
    }

    internal async Task<List<Fight>> LoadFFLogsFights(string reportId)
    {
        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, $"https://www.fflogs.com/reports/fights-and-participants/{reportId}/0"))
        {
            requestMessage.Headers.Referrer = new($"https://www.fflogs.com/reports/{reportId}");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var fightsAndParticipants = await response.Content.ReadFromJsonAsync<FightsAndParticipants>();

            foreach (var fight in fightsAndParticipants.Fights)
            {
                if (fight.ZoneId == 1000001)
                {
                    // FFLogs is returning a bad territory ID for forked tower; this hack corrects for that.
                    if (fight.ZoneName == "the Occult Crescent: South Horn")
                    {
                        fight.ZoneId = 1252;
                    }
                }
            }
            return fightsAndParticipants.Fights;
        }
    }

    internal async Task<WaymarkPreset> LoadFFLogsMarkers(string reportId, Fight fight)
    {
        uint boss = fight.Boss;
        uint startTime = fight.StartTime;
        uint endTime = fight.EndTime;

        using (HttpRequestMessage requestMessage = new(HttpMethod.Get, $"https://www.fflogs.com/reports/replaysegment/{reportId}/{boss}/{startTime}/{endTime}"))
        {
            requestMessage.Headers.Referrer = new($"https://www.fflogs.com/reports/{reportId}?fight={fight.Id}");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var replaySegment = await response.Content.ReadFromJsonAsync<ReplaySegment>();
            return replaySegment.GetPreset(fight);
        }
    }
    public class DungeonPull
    {
        public uint Id { get; set; }
        [JsonPropertyName("start_time")]
        public uint StartTime { get; set; }
        [JsonPropertyName("end_time")]
        public uint EndTime { get; set; }
    }
    public class Fight
    {
        public uint Id { get; set; }
        public uint Boss { get; set; }
        public uint ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        [JsonPropertyName("start_time")]
        public uint StartTime { get; set; }
        [JsonPropertyName("end_time")]
        public uint EndTime { get; set; }
        public List<DungeonPull> DungeonPulls { get; set; } = new();

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
    public class Map
    {
        public uint MapId { get; set; }
    }
    public class ReplaySegment
    {
        public List<WorldMarker> WorldMarkers { get; set; } = new();
        public List<Event> Events { get; set; } = new();
        public List<Map> Maps { get; set; } = new();

        public WaymarkPreset GetPreset(Fight fight)
        {
            var territoryId = (ushort)fight.ZoneId;

            if (!TerritorySheet.IsValid(territoryId))
            {
                throw new InvalidOperationException($"Illegal Territory ID: {fight.ZoneId}");
            }

            WaymarkPreset preset = new(
                name: $"FFLogs Fight {fight.Id}",
                territoryId: territoryId
            );

            foreach (var marker in WorldMarkers)
                if (Maps.Select(x => x.MapId).Contains(marker.MapId))
                    preset.MarkerPositions[marker.Waymark] = marker.Position;
            foreach (var event_ in Events)
                if (event_.IsWorldMarkerPlaced)
                    preset.MarkerPositions[event_.Waymark] = event_.Position;
                else if (event_.IsWorldMarkerRemoved)
                    preset.MarkerPositions.Remove(event_.Waymark);
            preset.MarkPendingHeightAdjustment();

            if (preset.MarkerPositions.Count == 0)
            {
                throw new InvalidOperationException("Empty waymark list");
            }

            return preset;
        }
    }
}
