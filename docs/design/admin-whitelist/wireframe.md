# Admin Whitelist Management Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  MudText Typo="h4"    Whitelist Management                          │
│  MudText Typo="body2" Manage user access                            │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  MudTextField Adornment=Search           MudButton            │  │
│  │  [Search users...__________________________]  [ + Add User ]  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudPaper ────────────────────────────────────────────────────┐  │
│  │  MudDataGrid Filterable Hover Striped                          │  │
│  │  ┌────────────────┬──────────┬────────────┬────────┬────────┐  │  │
│  │  │ Email          │ Added    │ Added By   │ Status │Actions │  │  │
│  │  ├────────────────┼──────────┼────────────┼────────┼────────┤  │  │
│  │  │ alice@mail.com │ 01-15    │ admin@co   │MudChip │[⚙][🗑]│  │  │
│  │  │                │          │            │ Active │        │  │  │
│  │  │ bob@mail.com   │ 02-01    │ admin@co   │MudChip │[⚙][🗑]│  │  │
│  │  │                │          │            │ Active │        │  │  │
│  │  │ carol@mail.com │ 03-10    │ admin@co   │MudChip │[⚙][🗑]│  │  │
│  │  │                │          │            │Disabled│        │  │  │
│  │  └────────────────┴──────────┴────────────┴────────┴────────┘  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudDialog "Add User" ────────────────────────────────────────┐  │
│  │  MudTextField Label="Email"                                    │  │
│  │  [user@example.com_________________________________]           │  │
│  │                                                                │  │
│  │  MudSelect Label="Role"                                        │  │
│  │  [User ▾]                                                      │  │
│  │                                                                │  │
│  │  MudSwitch [●] Send welcome email                              │  │
│  │                                                                │  │
│  │                            MudButton [Cancel]  MudButton [Add] │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ MudDialog "Remove User?" ────────────────────────────────────┐  │
│  │  MudAlert Severity=Warning                                     │  │
│  │  Remove alice@mail.com? They will lose access.                 │  │
│  │                                                                │  │
│  │                         MudButton [Cancel]  MudButton [Remove] │  │
│  └────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

- `MudDataGrid` with search/filter, striped rows, hover highlight
- `MudChip` — green for Active, grey for Disabled
- `MudDialog` — Add User with email, role select, welcome email toggle
- `MudDialog` — Remove confirmation with `MudAlert` warning

## Interactions

- Search filters the grid client-side
- Add User: validates email via FluentValidation, enqueues welcome email if toggled
- Toggle status: immediate toggle via Identity module, `MudSnackbar` feedback
- Remove: confirmation dialog, then delete via Identity module
- Admin-only page — `[Authorize(Roles = "Admin")]`
