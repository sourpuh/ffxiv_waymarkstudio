using System.Collections.Generic;
using System.Linq;

namespace WaymarkStudio;
internal class PresetDirectory
{
    internal string name = "";
    internal List<WaymarkPreset> presets = new();
    internal List<PresetDirectory> children = new();

    internal IEnumerable<WaymarkPreset> RecursiveListPresets()
    {
        IEnumerable<WaymarkPreset> allPresets = presets;
        foreach (var child in children)
        {
            allPresets = allPresets.Concat(child.RecursiveListPresets());
        }
        return allPresets;
    }
}
