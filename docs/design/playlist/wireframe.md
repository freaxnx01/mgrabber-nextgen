# Playlist Download Page Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    Playlist Download                             │
│  MudText Typo="body2" Download entire YouTube playlists             │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  MudTextField                                   MudButton      │  │
│  │  [https://youtube.com/playlist?list=..._____] [Load Playlist]  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  ┌──────┐  MudText Typo="h5"   Queen Greatest Hits            │  │
│  │  │thumb │  MudText Typo="body2" by Queen Official · 20 videos │  │
│  │  └──────┘                                                      │  │
│  │                                                                │  │
│  │  MudSelect          MudSwitch             MudButton            │  │
│  │  Format: [MP3 ▾]    [●] Normalize Audio   [ Download (18) ]   │  │
│  │                                                                │  │
│  │  ┌─ QuotaIndicator ────────────────────────────────────────┐   │  │
│  │  │  MudProgressLinear ████████████░░░░░░ 45%  Color=Info    │   │  │
│  │  │  450 MB / 1000 MB · Estimated: 180 MB                    │   │  │
│  │  └──────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  │  MudDataGrid  Filterable=false  SelectOnRowClick               │  │
│  │  ┌───┬────┬─────────┬────────────────────┬────────┬──────┐     │  │
│  │  │[✓]│ #  │ Thumb   │ Title              │Channel │      │     │  │
│  │  ├───┼────┼─────────┼────────────────────┼────────┼──────┤     │  │
│  │  │[✓]│  1 │ [img]   │ Bohemian Rhapsody  │ Queen  │ [⬇]  │     │  │
│  │  │[✓]│  2 │ [img]   │ Under Pressure     │ Queen  │ [⬇]  │     │  │
│  │  │[ ]│  3 │ [img]   │ We Will Rock You   │ Queen  │ [⬇]  │     │  │
│  │  └───┴────┴─────────┴────────────────────┴────────┴──────┘     │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper "Download Progress" ────────────────────────────────┐  │
│  │  (DownloadProgressCard components — same as Home page)         │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `QuotaIndicator` — reusable, shows `MudProgressLinear` with threshold coloring + estimated space needed
- `MudDataGrid` with multi-select checkboxes, thumbnail column, single-download button per row
- `DownloadProgressCard` — reused from Home, per-track progress via SignalR

## Interactions

- Paste URL → Load Playlist fetches metadata + video list
- All videos selected by default, user can toggle
- Format and normalize options apply to entire batch
- Download All enqueues one Hangfire job per selected video
- Individual download button per row for single-track download
