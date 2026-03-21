# MusicBrainz Search Page Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    MusicBrainz Search                            │
│  MudText Typo="body2" Search artists, tracks, albums — YouTube      │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  MudTextField                                                  │  │
│  │  [Search for artist, track, or album..._______]                │  │
│  │                                                                │  │
│  │  MudToggleGroup T="string" @bind-Value="SearchType"            │  │
│  │  [ Artist | Track | Album ]                                    │  │
│  │                                                                │  │
│  │  MudButton [ Search ]   MudButton Variant=Text [ Clear ]      │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ──────────────────────── MudChip "12 found" ────────┐  │
│  │  MudText "Results for 'Radiohead'"                             │  │
│  │                                                                │  │
│  │  MudList                                                       │  │
│  │  ┌──────────────────────────────────────────────────────────┐  │  │
│  │  │  Radiohead                                                │  │  │
│  │  │  MudChip[GB]  MudChip[Group]        MudButton [YouTube]  │  │  │
│  │  ├──────────────────────────────────────────────────────────┤  │  │
│  │  │  Radiohead Tribute Band                                   │  │  │
│  │  │  MudChip[US]  MudChip[Group]        MudButton [YouTube]  │  │  │
│  │  └──────────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ──────────────── MudIconButton [✕] ─────────────────┐  │
│  │  MudText "YouTube Results for 'Radiohead'"                     │  │
│  │                                                                │  │
│  │  (YouTubeResultsList component — same as Home page)            │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `MudToggleGroup` — switches between Artist, Track, Album search types
- `MudList` — displays results with metadata chips (country, type, disambiguation)
- `YouTubeResultsList` — reused, appears inline when user clicks YouTube button
- `MudChip` — result count badge

## Interactions

- Search queries MusicBrainz via Discovery module (rate-limited 1 req/sec)
- YouTube button on any result triggers YouTube search, shows inline results
- Download from YouTube results navigates to Home page
- Clear resets all state
