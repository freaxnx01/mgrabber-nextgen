# Layout Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│ MudAppBar                                                           │
│  [≡]  MusicGrabber                                   [avatar] ▾    │
├──────┬──────────────────────────────────────────────────────────────┤
│ Mud  │                                                              │
│Drawer│  MudMainContent                                              │
│      │                                                              │
│ Home │         << Page Content >>                                   │
│      │                                                              │
│ Play-│                                                              │
│ list │                                                              │
│      │                                                              │
│ Music│                                                              │
│Brainz│                                                              │
│      │                                                              │
│Radio │                                                              │
│      │                                                              │
│Pro-  │                                                              │
│file  │                                                              │
│      │                                                              │
│──────│                                                              │
│ADMIN │                                                              │
│White-│                                                              │
│list  │                                                              │
│Stats │                                                              │
│      │                                                              │
│──────│                                                              │
│[Jobs]│  Hangfire dashboard link (Admin only)                        │
└──────┴──────────────────────────────────────────────────────────────┘
```

## Components

- `MudLayout` with `MudAppBar` + `MudDrawer` + `MudMainContent`
- Drawer: `MudNavMenu` with `MudNavLink` items
- Admin section visible only to Admin role
- Hangfire dashboard link (`/hangfire`) for Admin only
- `MudAvatar` with user menu dropdown (Profile, Logout)
