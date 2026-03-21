# Home Page Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    Music Downloader                              │
│  MudText Typo="body2" Search and download music from YouTube        │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  MudTextField          MudButton                               │  │
│  │  [Search for songs, artists...______________]  [ Search ]      │  │
│  │                                                                │  │
│  │  ┌─ MudList ────────────────────────────────────────────────┐  │  │
│  │  │  MudListItem                                              │  │  │
│  │  │  ┌──────┐  Bohemian Rhapsody - Official Video             │  │  │
│  │  │  │thumb │  Queen · 5:55                                   │  │  │
│  │  │  └──────┘                  MudIconButton   MudMenu        │  │  │
│  │  │                            [Download]      [MP3 ▾]        │  │  │
│  │  │                            Color=Success   [FLAC]         │  │  │
│  │  │                                            [M4A]          │  │  │
│  │  ├──────────────────────────────────────────────────────────┤  │  │
│  │  │  ┌──────┐  Bohemian Rhapsody (Remastered 2011)            │  │  │
│  │  │  │thumb │  Queen · 5:54                                   │  │  │
│  │  │  └──────┘                  MudIconButton [Download]       │  │  │
│  │  └──────────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  MudText "Active Downloads"                                    │  │
│  │                                                                │  │
│  │  ┌─ DownloadProgressCard ─────────────────────────────────┐    │  │
│  │  │  Bohemian Rhapsody          Downloading                 │    │  │
│  │  │  MudProgressLinear ████████████░░░░░░ 72%  Color=Prim   │    │  │
│  │  ├─────────────────────────────────────────────────────────┤    │  │
│  │  │  Under Pressure             Pending                     │    │  │
│  │  │  MudProgressLinear ░░░░░░░░░░░░░░░░░  0%  Color=Def    │    │  │
│  │  └─────────────────────────────────────────────────────────┘    │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  MudText "Your Files"                                          │  │
│  │                                                                │  │
│  │  MudDataGrid                                                   │  │
│  │  ┌────────────────┬────────┬────────┬────────┬─────────────┐   │  │
│  │  │ Title          │ Artist │ Size   │ Format │ Actions     │   │  │
│  │  ├────────────────┼────────┼────────┼────────┼─────────────┤   │  │
│  │  │ Bohemian Rhaps │ Queen  │ 8.2 MB │ MP3    │ [⬇] [🗑]   │   │  │
│  │  │ Under Pressure │ Queen  │ 6.1 MB │ MP3    │ [⬇] [🗑]   │   │  │
│  │  │ Don't Stop Me  │ Queen  │ 5.4 MB │ FLAC   │ [⬇] [🗑]   │   │  │
│  │  └────────────────┴────────┴────────┴────────┴─────────────┘   │  │
│  │  MudPagination                                    [< 1 2 >]    │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `YouTubeResultsList` — reusable, shows search results with thumbnail, title, author, duration, download button with format menu
- `DownloadProgressCard` — reusable, shows job title, status, `MudProgressLinear` updated via SignalR
- `MudDataGrid` — paginated file list with download-to-device and delete actions
- `MudSnackbar` — replaces inline alerts for user feedback

## Interactions

- Search triggers YouTube API via Download module service
- Download button enqueues Hangfire job, progress pushed via SignalR
- File download triggers browser file download
- Delete shows `MudDialog` confirmation first
