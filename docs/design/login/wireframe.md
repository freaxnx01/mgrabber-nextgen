# Login Page Wireframe (Static SSR)

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│                                                                     │
│                  MudPaper Elevation=4 MaxWidth=400                   │
│                  ┌──────────────────────────────┐                   │
│                  │                              │                   │
│                  │  MudIcon Music Size=XLarge    │                   │
│                  │                              │                   │
│                  │  MudText Typo="h4"            │                   │
│                  │  MusicGrabber                 │                   │
│                  │                              │                   │
│                  │  MudButton FullWidth          │                   │
│                  │  StartIcon=Google             │                   │
│                  │  [ Login with Google ]        │                   │
│                  │                              │                   │
│                  │  MudText Typo="caption"       │                   │
│                  │  Access requires an active    │                   │
│                  │  whitelist entry.             │                   │
│                  │                              │                   │
│                  └──────────────────────────────┘                   │
│                                                                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- Centered `MudPaper` with elevation for card effect
- `MudIcon` — music note, large
- `MudButton` — full width, Google icon, triggers OAuth flow
- `MudText` caption — whitelist requirement notice

## Notes

- Static SSR — no interactivity needed, just a link to the OAuth endpoint
- No MudDrawer or MudAppBar — standalone page, no navigation
