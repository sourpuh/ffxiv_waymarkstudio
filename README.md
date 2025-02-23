# Waymark Studio

Waymark Studio (WMS) is a total replacement for the in-game waymark editor and Waymark Preset plugin. Waymark Studio provides a suite of tools to make placing and loading waymark presets simple, fast, and accurate.

<img align="right" height=350 src="/ReadmeImages/window.png">

Features include:
1. Set up visual guides to place waymarks on a grid
1. Shortcuts to place waymarks on clockspots
1. Place draft waymarks in duty recorder and during combat
1. Save and Load presets outside of instances (open world, Eureka, Bozja, etc.)
1. Mouse hover preset preview
1. Place from a suite of packaged community created presets
1. Import waymarks from FFLogs reports
1. Seamlessly share waymarks between Criterion Normal and Savage
1. Automatically load presets when entering an instance or dungeon region (planned)

> [!CAUTION]
>Use at your own risk.
>
> Waymark Studio tries to prevent you from accidentally placing illegitimate (floating or out-of-bounds) waymarks, but it is possible in some scenarios. Square Enix can detect and punish those who illegitimately place waymarks (https://eu.finalfantasyxiv.com/lodestone/news/detail/1e7aea492d72d81e2b3dbcfcdd3dacf07111126b).
>
>While you're unlikely to be detected or punished, if you want to place illegitimate waymarks and plan to spread them in Party Finder, it is safer to create the preset with an alt account and place it in PF parties before using it on a main account.
>
> Waymark Studio allows you to save and place presets within the open world or instances where presets are not supported by the game. Placing these presets will be slower and distance limited by default to be undetectable. You can disable safety checks for these presets in the settings to place them immediately and at any range. With this disabled, preset placement will be obviously impossible and be theoretically detectable; use at your own risk.

## Drafts
Waymark Studio uses a special form of waymarks called 'draft' markers.
Draft markers are only visible to you, show the precise size of a marker, and can be placed in combat or duty recorder.
Once you place your draft markers as desired, you can convert them to real waymarks or save them as a preset with a single button press.
By default, WMS will place real waymarks if possible and fallback to draft markers, but it can be configured to always place draft markers by unchecking "Place real markers if possible".

<img height=200 src="/ReadmeImages/circle_guide_withclockspots.png">

## Guides
Guides are customizable line grids that you can place on the arena to help you place waymarks at precise positions; they are only visible to you. WMS provides two types of guides (circular and rectangular) to operate on typical FFXIV boss arena shapes. WMS has shortcuts to place draft waymarks at clockspots on the guide.

<img height=350 src="/ReadmeImages/circle_guide.png">
<img height=350 src="/ReadmeImages/square_guide.png">

## Export and Import
To export a preset, right click the preset and copy it to your clipboard for sharing. It should look something like `wms08wInAT9VAMmqAtauAgD/AQgAxKkC464CADacXwDZBJNeAKsDD084UyAtIEdvZCBLZWZrYQ==` in your clipboard.

To import a preset, copy the preset to your clipboard. An import button will appear next to the "Saved Presets" heading that will load the preset from your clipboard when pressed.

To import a preset from FFLogs:
1. Copy a report URL to your clipboard. It should look something like `https://www.fflogs.com/reports/dY98jQtfJkAWLaZ3?fight=16`.
1. Press the blue gem button next to the "Saved Presets" heading to open the importer.
1. Paste the URL and press '✔' to begin the import.
1. If your URL does not have a `fight=N` parameter, you will be prompted to choose a fight from the report.
1. A chat message will indicate whether or not the import succeeded.
1. FFLogs does not provide "height" coordinates for waymarks, so WMS determines height for you when you enter the corresponding zone.

## Community Presets
WMS contains a set of community presets from https://github.com/Em-Six/FFXIVWaymarkPresets/wiki. If you have a preset that you want to include in community presets, follow the support process to suggest the preset.

## Installation
1. Open Dalamud settings using your preferred method, such as the `/xlsettings` chat command.
1. Open the "Experimental" tab.
1. Paste `https://puni.sh/api/repository/sourpuh` to the last empty box in the "Custom Plugin Repositories" section.
1. Press '+' and then '💾'
1. Search for `Waymark Studio` in the plugin installer and install the plugin
1. WMS can be accessed with the chat command `/wms` or by opening the game's waymark editor.

## Support
If you found a bug or have suggestions for the plugin, please do one of the following:

1. Check if a [GitHub issue](https://github.com/sourpuh/ffxiv_waymarkstudio/issues) already exists for the same thing.
1. Create a [new GitHub issue](https://github.com/sourpuh/ffxiv_waymarkstudio/issues/new). Provide a detailed description of the suggestion or problem (For bugs, include logs or steps to reproduce the issue).
1. Ask in [Discord](https://discord.gg/punishxiv): it might be a known issue or people might be able to help you quickly.
1. Message `@sourpuh` on Discord for direct author support.
