# Radio Now Playing Page Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    Radio Now Playing                             │
│  MudText Typo="body2" Swiss radio stations — extract audio          │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  StationSelector                                               │  │
│  │  MudToggleGroup T="string" @bind-Value="SelectedStation"       │  │
│  │  [ Radio SRF 1 | *Radio SRF 3* | Radio SRF Virus ]            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper Elevation=3 ──────────────────────────────── NOW ───┐  │
│  │                                                                │  │
│  │  MudText Typo="h3"     Bohemian Rhapsody       ┌───────────┐  │  │
│  │  MudText Typo="h5"     Queen                    │ MudIcon   │  │  │
│  │                                                  │ Music     │  │  │
│  │  MudChip[5:55]  MudChip[Started: 14:32]         │ Note      │  │  │
│  │                                                  └───────────┘  │  │
│  │  MudButton Color=Success Size=Large                             │  │
│  │  [ Extract Audio ]                                              │  │
│  │                                                                │  │
│  │  MudButton Variant=Outlined                                     │  │
│  │  [ Search YouTube ]                                             │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ──────────────────────── MudChip "20 songs" ────────┐  │
│  │  MudText "Recent Playlist"                                     │  │
│  │                                                                │  │
│  │  MudList                                                       │  │
│  │  ┌──────────────────────────────────────────────────────────┐  │  │
│  │  │  Under Pressure                                          │  │  │
│  │  │  Queen · 14:26                     [Extract] [YouTube]   │  │  │
│  │  ├──────────────────────────────────────────────────────────┤  │  │
│  │  │  Don't Stop Me Now                                       │  │  │
│  │  │  Queen · 14:22                     [Extract] [YouTube]   │  │  │
│  │  └──────────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudDialog ───────────────────────────────────────────────────┐  │
│  │  "YouTube Results for 'Queen Bohemian Rhapsody'"    [Close ✕] │  │
│  │  (YouTubeResultsList component)                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `StationSelector` — reusable, `MudToggleGroup` for station selection
- Now-playing card — elevated `MudPaper`, prominent display of current song
- `MudList` — recent playlist with extract/YouTube buttons per song
- `MudDialog` — YouTube results modal using `YouTubeResultsList`

## Interactions

- Station selection loads now-playing + recent playlist from Radio module
- Auto-refresh every 30 seconds via client-side Timer
- Extract Audio: auto-searches YouTube → best match → enqueues download
- If no good match, falls back to MudDialog with YouTube results for manual pick
- Search YouTube: opens MudDialog directly
