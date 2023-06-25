NowPlayingJellyfin.dll
================

A Rainmeter plugin to read the currently playing track on Jellyfin.


# Usage

The plugin works similarly to the [NowPlaying measure](https://docs.rainmeter.net/manual/measures/nowplaying) : there is a main measure doing the query and child measures connected to the main.


# Options

### `JellyfinToken` (required on main measure)

_Main measure only._ Your API token (can be created in Jellyfin advanced setttings).

### `JellyfinServer` (default: `http://localhost:8096`)

_Main measure only._ The url of your Jellyfin server.

### `JellyfinUsername`

_Main measure only._ Your Jellyfin username, this is used to filter the sessions if other users are using your server. If not defined, the first session will be used.

### `PlayerName` (required on child measures)

_Child measures only._ Name of the main measure.

### `PlayerType` (required)

Type of the measure value. Valid values are: 

- `Artist` : Track artist.
- `Album` : Current album.
- `Title` : Track title.
- `Number` : Track number.
- `Year` : Track year.
- `Cover` : URL to cover art.
- `File` : Path to the playing media file.
- `Duration` : Total length of track in seconds.
- `Position` : Current position in track in seconds.
- `Progress` : Percentage of track completed.
- `State` : 0 for stopped, 1 for playing, and 2 for paused.

**Notes:** With measures of type `Duration` or `Position`, the string value is in the form `MM:SS` and the number value is the actual number of seconds.

### `DisableLeadingZero` (default: `0`)

_Main measure only._  If set to `1`, the format of `Duration` and `Position` is `M:SS` instead of `MM:SS`.


# Example

```ini
[Rainmeter]
Update=1000

[Variables]
JellyfinToken=XXXXXX
JellyfinUsername=JohnDoe

[MeasureTitleJellyfin]
Measure=Plugin
Plugin=NowPlayingJellyfin
JellyfinToken=#JellyfinToken#
JellyfinUsername=#JellyfinUsername#
PlayerType=Title

[MeasureArtistJellyfin]
Measure=Plugin
Plugin=NowPlayingJellyfin
PlayerName=MeasureTitleJellyfin
PlayerType=Artist

[MeasureAlbumJellyfin]
Measure=Plugin
Plugin=NowPlayingJellyfin
PlayerName=MeasureTitleJellyfin
PlayerType=Album

[MeasureDurationJellyfin]
Measure=Plugin
Plugin=NowPlayingJellyfin
PlayerName=MeasureTitleJellyfin
PlayerType=Duration
Substitute="00:00":""

[MeasurePositionJellyfin]
Measure=Plugin
Plugin=NowPlayingJellyfin
PlayerName=MeasureTitleJellyfin
PlayerType=Position
Substitute="00:00":""
```


# Developpement

This plugin uses `Newtonsoft.Json` library. When doing a release it is bundled into the plugin DDL with `ILMerge`. When developping you need to copy `Newtonsoft.Json.dll` into the Rainmeter folder (next to `Rainmeter.exe`).


# License

MIT
