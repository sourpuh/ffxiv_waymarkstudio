namespace WaymarkStudio;

internal record struct TerritoryFilter
{
    public Expansion? SelectedExpansion;
    public ContentType? SelectedContentType;

    internal void Toggle(Expansion expansion)
    {
        if (SelectedExpansion == expansion)
            SelectedExpansion = null;
        else
            SelectedExpansion = expansion;
    }

    internal void Toggle(ContentType ct)
    {
        if (SelectedContentType == ct)
            SelectedContentType = null;
        else
            SelectedContentType = ct;
    }

    internal bool IsTerritoryFiltered(ushort territoryId)
    {
        if (territoryId == 0) return true;

        var expansion = TerritorySheet.GetExpansion(territoryId);
        if (SelectedExpansion != null && SelectedExpansion != expansion)
            return true;

        var contentType = TerritorySheet.GetContentType(territoryId);
        if (SelectedContentType != null && SelectedContentType != contentType)
            return true;

        return false;
    }
}
