# Search & Download Page - Wireframe (Phase 1)

**Feature:** Search YouTube and download audio  
**Route:** `/` (Home)  
**Component:** `Home.razor`  
**Created:** Retroactively (2026-03-14)  
**Status:** ✅ Implemented

---

## Layout Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  🎵 Music Downloader                                                │
│  Search and download music from YouTube                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Search YouTube                                             │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  ┌────────────────────────────────────┐  ┌──────────────┐  │   │
│  │  │ Search for songs, artists...       │  │   Search     │  │   │
│  │  └────────────────────────────────────┘  └──────────────┘  │   │
│  │                                                             │   │
│  │  ┌─────────────────────────────────────────────────────┐   │   │
│  │  │ ▶️ Song Title 1                    [Download]       │   │   │
│  │  │    Artist • 3:45                                        │   │
│  │  ├─────────────────────────────────────────────────────┤   │   │
│  │  │ ▶️ Song Title 2                    [Download]       │   │   │
│  │  │    Artist • 4:12                                        │   │
│  │  ├─────────────────────────────────────────────────────┤   │   │
│  │  │ ▶️ Song Title 3                    [Download]       │   │   │
│  │  │    Artist • 3:28                                        │   │
│  │  └─────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Your Downloads                                             │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  ┌─────────────────────────────────────────────────────┐   │   │
│  │  │ Title      │ Artist    │ Size  │ Status │ Actions  │   │   │
│  │  ├────────────┼───────────┼───────┼────────┼──────────┤   │   │
│  │  │ The Look   │ Roxette   │ 4.2MB │   ✓    │ [Delete] │   │   │
│  │  │ Song 2     │ Artist 2  │ 3.8MB │   ✓    │ [Delete] │   │   │
│  │  └─────────────────────────────────────────────────────┘   │   │
│  │                                                             │   │
│  │  [Empty state: "No downloads yet. Search and download..."] │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Components Breakdown

### 1. Header Section
- **Title:** "🎵 Music Downloader" (h1)
- **Subtitle:** "Search and download music from YouTube"

### 2. Alert/Message Area
- **Type:** Bootstrap alert (success/danger/info)
- **Position:** Below subtitle, above search card
- **Trigger:** User actions (search error, download started, etc.)

### 3. Search Card
- **Header:** "Search YouTube"
- **Input:** Text field with placeholder
- **Button:** Primary "Search" button with loading spinner
- **Results:** List group with video items
  - Title (h6)
  - Author • Duration (small text)
  - Download button (success style)

### 4. Downloads Card
- **Header:** "Your Downloads"
- **Table Columns:** Title, Artist, Size, Status, Actions
- **Status Badge:** Green "Available"
- **Action:** Delete button (danger style)
- **Empty State:** Helpful message when no downloads

---

## Empty States

### No Search Results
```
┌─────────────────────────────────────────────────────────┐
│  [Search input] [Search button]                         │
│                                                         │
│  (no results shown - just empty space)                  │
└─────────────────────────────────────────────────────────┘
```

### No Downloads
```
┌─────────────────────────────────────────────────────────┐
│  Your Downloads                                         │
├─────────────────────────────────────────────────────────┤
│  No downloads yet. Search and download some music!      │
│  (text-muted)                                           │
└─────────────────────────────────────────────────────────┘
```

---

## Loading States

### Search Loading
- Button shows spinner: `<span class="spinner-border spinner-border-sm">`
- Button disabled during search
- Text hidden, spinner visible

### Download Loading (Implicit)
- Alert shows "Download started for: {title}"
- No explicit spinner on download button (instant feedback via alert)

---

## Responsive Behavior

| Screen | Layout |
|--------|--------|
| Desktop (>992px) | Full two-column potential, currently single column |
| Tablet (768px) | Single column, full width cards |
| Mobile (<576px) | Stacked layout, search input full width |

---

## Color Scheme

| Element | Bootstrap Class |
|---------|-----------------|
| Primary buttons | `btn-primary` |
| Success/Download | `btn-success` |
| Danger/Delete | `btn-danger` |
| Alerts | `alert-success`, `alert-danger`, `alert-info` |
| Badges | `bg-success` |
| Cards | `card`, `card-header`, `card-body` |

---

**Approval Status:** ✅ Retroactively documented (originally built without wireframe)
