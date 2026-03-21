# Admin Statistics Dashboard Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    Statistics                                    │
│  MudText Typo="body2" System usage overview                         │
│                                                                     │
│  ┌─ MudGrid ─────────────────────────────────────────────────────┐  │
│  │  MudItem x4                                                    │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐      │  │
│  │  │  1,247   │  │ 12.4 GB  │  │    8     │  │  91.2%   │      │  │
│  │  │  Total   │  │ Storage  │  │ Active   │  │ Success  │      │  │
│  │  │Downloads │  │  Used    │  │Users(7d) │  │  Rate    │      │  │
│  │  │Color=Prim│  │Color=Succ│  │Color=Info│  │Color=Warn│      │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘      │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper "Status Distribution" ──────────────────────────────┐  │
│  │  MudChip Color=Success [Completed 1137]                        │  │
│  │  MudChip Color=Error   [Failed 82]                             │  │
│  │  MudChip Color=Warning [Pending 18]                            │  │
│  │  MudChip Color=Info    [Downloading 10]                        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper "Downloads Per Day (30 days)" ──────────────────────┐  │
│  │  MudSimpleTable                                                │  │
│  │  2026-03-21  12  MudProgressLinear ████████████░░░░░           │  │
│  │  2026-03-20  18  MudProgressLinear ██████████████████           │  │
│  │  2026-03-19   8  MudProgressLinear ████████░░░░░░░░░           │  │
│  │  2026-03-18  22  MudProgressLinear ██████████████████████       │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper "Users" ────────────────────────────────────────────┐  │
│  │  MudDataGrid                                                   │  │
│  │  ┌────────┬───────┬────────┬──────┬──────┬──────────┬───────┐  │  │
│  │  │ User   │ Total │Storage │ Done │ Fail │ Active   │       │  │  │
│  │  ├────────┼───────┼────────┼──────┼──────┼──────────┼───────┤  │  │
│  │  │alice.. │  312  │ 3.2 GB │  298 │   14 │ 03-21    │[Detail│  │  │
│  │  │bob..   │  189  │ 1.8 GB │  180 │    9 │ 03-20    │[Detail│  │  │
│  │  └────────┴───────┴────────┴──────┴──────┴──────────┴───────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudDialog "User Details: alice@mail.com" ────────────────────┐  │
│  │  MudGrid                                                       │  │
│  │  ┌────────┐  ┌────────┐  ┌────────┐                            │  │
│  │  │  312   │  │ 3.2 GB │  │ 95.5%  │                            │  │
│  │  │ Total  │  │Storage │  │Success │                            │  │
│  │  └────────┘  └────────┘  └────────┘                            │  │
│  │                                                                │  │
│  │  MudText "Top Artists"                                         │  │
│  │  MudSimpleTable                                                │  │
│  │  Queen                             MudChip [42 downloads]      │  │
│  │  Radiohead                         MudChip [28 downloads]      │  │
│  │  Pink Floyd                        MudChip [19 downloads]      │  │
│  │                                              MudButton [Close] │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `MudPaper` stat cards — colored backgrounds per metric type
- `MudChip` — status distribution with semantic colors
- `MudSimpleTable` with inline `MudProgressLinear` — downloads-per-day trend
- `MudDataGrid` — user stats with detail button
- `MudDialog` — user detail drill-down with top artists

## Interactions

- Page loads global stats from Download + Quota modules, aggregated in frontend service
- User stats loaded from Download module
- Detail button opens `MudDialog` with per-user stats from Download module
- Admin-only page — `[Authorize(Roles = "Admin")]`
