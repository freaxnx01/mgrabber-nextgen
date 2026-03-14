# Admin Statistics Page - Wireframe (Phase 1)

**Feature:** Admin dashboard with system statistics  
**Route:** `/admin/statistics`  
**Component:** `Statistics.razor`  
**Authorization:** Admin or SuperAdmin role required  
**Created:** Retroactively (2026-03-14)  
**Status:** ✅ Implemented

---

## Layout Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  📊 Admin Statistics                                                │
│  Overview of system usage and download statistics                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐       │
│  │ Total      │ │ Storage    │ │ Active     │ │ Success    │       │
│  │ Downloads  │ │ Used       │ │ Users (7d) │ │ Rate       │       │
│  │            │ │            │ │            │ │            │       │
│  │  1,234     │ │ 456.7 MB   │ │    42      │ │   98.5%    │       │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘       │
│           [bg-primary]   [bg-success]   [bg-info]   [bg-warning]   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Download Status Distribution                               │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  [Completed] 1,200    [Pending] 15    [Failed] 19           │   │
│  │  [Processing] 45      [Cancelled] 5                         │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Downloads Per Day (Last 30 Days)                           │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  Date          │ Downloads │ Visual Bar                     │   │
│  │  ──────────────┼───────────┼────────────────────────────    │   │
│  │  2026-03-14    │    45     │ ████████████████████           │   │
│  │  2026-03-13    │    38     │ ████████████████               │   │
│  │  2026-03-12    │    52     │ ███████████████████████        │   │
│  │  ...           │    ...    │ ...                            │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  User Statistics                                            │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  User ID      │ Downloads │ Storage    │ Top Artist        │   │
│  │  ─────────────┼───────────┼────────────┼────────────────   │   │
│  │  user1        │    25     │ 120.5 MB   │ Roxette           │   │
│  │  user2        │    18     │ 89.3 MB    │ Queen             │   │
│  │  ...          │    ...    │ ...        │ ...               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Top Artists                                                │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  Artist              │ Downloads │ % of Total               │   │
│  │  ────────────────────┼───────────┼──────────────────        │   │
│  │  Roxette             │    45     │ ████████████ 15%         │   │
│  │  Queen               │    38     │ ██████████ 12%           │   │
│  │  ...                 │    ...    │ ...                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Components Breakdown

### 1. Header Section
- **Title:** "📊 Admin Statistics" (h1)
- **Subtitle:** "Overview of system usage and download statistics"

### 2. Alert/Message Area
- **Type:** Bootstrap alert (danger for errors)
- **Position:** Below subtitle
- **Trigger:** API errors

### 3. Stats Cards Row
4-column grid with colored cards:

| Card | Color | Metric | Data Source |
|------|-------|--------|-------------|
| Total Downloads | bg-primary | `GlobalStats.TotalDownloads` | API |
| Storage Used | bg-success | `GlobalStats.TotalStorageMB` | API |
| Active Users (7d) | bg-info | `GlobalStats.ActiveUsersLast7Days` | API |
| Success Rate | bg-warning | Calculated from status counts | Derived |

### 4. Status Distribution Card
- **Header:** "Download Status Distribution"
- **Content:** Grid of status badges with counts
- **Statuses:** Completed, Pending, Failed, Processing, Cancelled

### 5. Downloads Per Day Card
- **Header:** "Downloads Per Day (Last 30 Days)"
- **Content:** Table with visual bar representation
- **Columns:** Date, Downloads, Bar

### 6. User Statistics Card
- **Header:** "User Statistics"
- **Content:** Table with user details
- **Columns:** User ID, Downloads, Storage, Top Artist

### 7. Top Artists Card
- **Header:** "Top Artists"
- **Content:** Table with percentage bars
- **Columns:** Artist, Downloads, % of Total

---

## Empty States

### No Data Yet (Fresh Install)
```
┌─────────────────────────────────────────────────────────┐
│  Total Downloads                                        │
│                                                         │
│  0                                                      │
└─────────────────────────────────────────────────────────┘
(All cards show 0 or empty tables)
```

### Loading State
```
┌─────────────────────────────────────────────────────────┐
│  Download Status Distribution                           │
├─────────────────────────────────────────────────────────┤
│  Loading... (no explicit spinner shown)                 │
└─────────────────────────────────────────────────────────┘
```

### Access Denied (Non-Admin)
- Handled by `@attribute [Authorize(Roles = "Admin,SuperAdmin")]`
- Redirects to login or shows access denied page

---

## Color Scheme

| Element | Bootstrap Class | Text Color |
|---------|-----------------|------------|
| Total Downloads card | `bg-primary` | `text-white` |
| Storage Used card | `bg-success` | `text-white` |
| Active Users card | `bg-info` | `text-white` |
| Success Rate card | `bg-warning` | `text-dark` |
| Status badges | Various | - |
| Tables | `table`, `table-sm` | - |
| Cards | `card`, `mb-4` | - |

---

## Responsive Behavior

| Screen | Layout |
|--------|--------|
| Desktop (>1200px) | 4 cards in row, full tables |
| Desktop (>992px) | 4 cards in row, scrollable tables |
| Tablet (768px) | 2x2 card grid, stacked sections |
| Mobile (<576px) | Single column, all cards stacked |

---

**Approval Status:** ✅ Retroactively documented (originally built without wireframe)
