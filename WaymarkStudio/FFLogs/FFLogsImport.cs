using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WaymarkStudio.FFLogs;
internal class FFLogsImport
{
    private readonly static FFLogsClient Client = new();

    internal static FFLogsImport New(string url)
    {
        return new(url);
    }

    public string URL = "";
    public int UserSelectedFightIndex = 0;
    public string[] FightArray = [];
    public Task<WaymarkPreset>? Task { get; private set; }

    private SemaphoreSlim continueSignal = new SemaphoreSlim(0, 1);
    private bool isQueryRunning = false;

    private FFLogsImport(string url)
    {
        URL = url;
    }

    internal bool CanQuery => !isQueryRunning;

    internal bool IsStarted => Task != null;

    internal bool IsCompleted => Task?.IsCompleted ?? false;

    internal void Start()
    {
        Task = ResultAsync();
    }

    private async Task<WaymarkPreset> ResultAsync()
    {
        (var reportId, var urlFightIndex, var position, var pull) = ParseUrl(URL);
        if (urlFightIndex >= 0)
        {
            UserSelectedFightIndex = urlFightIndex;
        }
        isQueryRunning = true;
        var fights = await Client.LoadFFLogsFights(reportId);
        isQueryRunning = false;

        if (urlFightIndex == -1)
        {
            FightArray = fights.Select(x => $"{x.ZoneName} {x.Id}").ToArray();
            await continueSignal.WaitAsync();
        }

        UserSelectedFightIndex = Math.Clamp(UserSelectedFightIndex, 0, fights.Count - 1);
        var userSelectedFight = fights[UserSelectedFightIndex];
        if (pull >= 0)
            userSelectedFight.EndTime = userSelectedFight.DungeonPulls.First(x => x.Id == pull).StartTime;
        else if (position > 0)
            userSelectedFight.EndTime = (uint)(userSelectedFight.StartTime + position);
        else
            userSelectedFight.EndTime = userSelectedFight.StartTime + 1;
        isQueryRunning = true;
        var preset = await Client.LoadFFLogsMarkers(reportId, userSelectedFight);
        isQueryRunning = false;

        Plugin.Chat.Print($"Successfully imported {preset.Name} for {userSelectedFight.ZoneName}. The import will finalize when you enter the zone, approach the waymark location, and attempt to place it.", Plugin.Tag, 45);

        return preset;
    }

    internal void Continue()
    {
        continueSignal.Release();
    }

    internal static (string reportId, int fightIndex, int position, int pull) ParseUrl(string ffLogsUrl)
    {
        if (ffLogsUrl == null || ffLogsUrl.Length == 0)
            throw new ArgumentException("Invalid URI: No URI was provided.");
        var uri = new Uri(ffLogsUrl);
        if (uri.Host != "www.fflogs.com")
            throw new ArgumentException("Invalid URI: The URI is not a valid FFLogs host.");
        if (!uri.AbsolutePath.StartsWith("/reports/"))
            throw new ArgumentException("Invalid URI: The URI is not an FFLogs report.");
        var reportId = uri.AbsolutePath.Substring("/reports/".Length);
        if (!Regex.IsMatch(reportId, "^[a-zA-Z0-9]+$"))
            throw new ArgumentException("Invalid URI: The URI has an invalid report ID.");

        var query = HttpUtility.ParseQueryString(uri.Query);
        var fightStr = query.Get("fight");

        var fightIndex = -1;
        if (fightStr != null)
        {
            if (fightStr == "last")
                fightIndex = int.MaxValue;
            else
                fightIndex = int.Parse(fightStr) - 1;
        }

        var positionStr = query.Get("position");
        var position = -1;
        if (positionStr != null)
            position = int.Parse(positionStr);

        var pullStr = query.Get("pull");
        var pull = -1;
        if (pullStr != null)
            pull = int.Parse(pullStr);

        return (reportId, fightIndex, position, pull);
    }
}
