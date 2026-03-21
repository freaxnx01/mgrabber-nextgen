# Profile Page Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    My Profile                                    │
│                                                                     │
│  ┌─ MudGrid ─────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  MudItem xs=12 md=4            MudItem xs=12 md=8             │  │
│  │  ┌─ MudPaper ───────────┐      ┌─ MudPaper ───────────────┐  │  │
│  │  │  MudText "Account"   │      │  MudGrid                  │  │  │
│  │  │                      │      │  ┌──────┐ ┌──────┐ ┌────┐ │  │  │
│  │  │  MudAvatar           │      │  │  42  │ │  38  │ │ 20 │ │  │  │
│  │  │  Color=Primary [DU]  │      │  │Total │ │ Done │ │Act │ │  │  │
│  │  │                      │      │  └──────┘ └──────┘ └────┘ │  │  │
│  │  │  Dev User            │      └────────────────────────────┘  │  │
│  │  │  dev@example.com     │                                      │  │
│  │  │  MudChip[Admin]      │      ┌─ MudPaper "Settings" ─────┐  │  │
│  │  │  Since: 2026-02-20   │      │  MudForm                   │  │  │
│  │  └──────────────────────┘      │                             │  │  │
│  │                                 │  MudSelect                  │  │  │
│  │  ┌─ MudPaper "Storage" ─┐      │  Format: [MP3 ▾]           │  │  │
│  │  │                      │      │                             │  │  │
│  │  │  QuotaIndicator      │      │  MudSwitch                  │  │  │
│  │  │                      │      │  [●] Normalize Audio        │  │  │
│  │  │  MudProgressLinear   │      │                             │  │  │
│  │  │  ████████░░░░░ 45%   │      │  MudSlider                  │  │  │
│  │  │  450 MB / 1000 MB    │      │  Target: [-14 LUFS ──●──]  │  │  │
│  │  │  12 files            │      │                             │  │  │
│  │  │  550 MB remaining    │      │  MudSwitch                  │  │  │
│  │  │                      │      │  [●] Email Notifications    │  │  │
│  │  └──────────────────────┘      │                             │  │  │
│  │                                 │  MudButton [Save Settings] │  │  │
│  │                                 └─────────────────────────────┘  │  │
│  │                                                                │  │
│  │                                 ┌─ MudPaper "Activity" ─────┐  │  │
│  │                                 │  MudText "Top Artists"     │  │  │
│  │                                 │  MudSimpleTable            │  │  │
│  │                                 │  Queen          8 dl       │  │  │
│  │                                 │  Radiohead      5 dl       │  │  │
│  │                                 │  Pink Floyd     3 dl       │  │  │
│  │                                 │                            │  │  │
│  │                                 │  MudText "Downloads (30d)" │  │  │
│  │                                 │  MudSimpleTable            │  │  │
│  │                                 │  2026-03-20     4          │  │  │
│  │                                 │  2026-03-19     2          │  │  │
│  │                                 └────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `MudGrid` — responsive 4/8 column split (stacks on mobile)
- `MudAvatar` — user initials with primary color
- `QuotaIndicator` — reused, storage bar with threshold warnings
- `MudForm` — settings form with `MudSelect`, `MudSwitch`, `MudSlider`
- `MudSimpleTable` — top artists and recent downloads

## Interactions

- Page loads profile, quota, stats, and settings from Identity + Quota + Download modules
- Save Settings calls Identity module `UpdateSettings` use case
- Success/failure feedback via `MudSnackbar`
- Normalization slider only visible when normalize switch is on
