# 06 — UI States (V1)

This document defines all application UI states, visible data, allowed actions, and state transitions.
No visual styling is defined here. Only behavior and interaction structure.

---

# 1. Global UI Structure

The application consists of:

- Top-level application shell
- Landing state (no active sources)
- Active workspace (with activity_sheet loaded)
- Event timeline view (multi-source)
- Daily Cycles view
- Day Workspace (activity segments editor)
- Event editor
- Recurrence editor
- Settings
- Sync status panel
- Conflict resolution dialog

Global UI elements always visible when a source is active:

- Connectivity indicator: Online / Offline
- Global sync indicator: total pending count
- Sync details access (opens per-source breakdown)
- Active sources indicator

---

# 2. State: Landing Window

Trigger:
- App launch
- User closes active workspace

Visible elements:
- Button: New Activity Source
- Button: Open Activity Source
- Section: Recent Activity Sources
- Section: Recent Event Sources

Each recent entry displays:
- Display name
- Last opened timestamp
- Indicator: Available Offline (if cache exists)

Actions:
- Create new activity source → F1 flow
- Open existing activity source → F3 flow
- Activate recent source → Load active workspace
- Add event source (optional without closing activity source)

Transition:
- On successful activity source load → Active Workspace

---

# 3. State: Active Workspace (Shell)

Precondition:
- Exactly one activity_sheet is active.

Layout (conceptual zones):
- Top bar (global status)
- Main content area
- Optional collapsible Sync Details panel

Main navigation (tabs or switch control):
- Events Timeline
- Daily Cycles
- Settings

Event sources:
- Multiple event sources may be active simultaneously.
- User may enable/disable event sources via toggle list.

---

# 4. State: Events Timeline View

Purpose:
Display all events from all active event sources.

Data sources:
- All active `google_sheets_events` sources.
- Recurrences expanded dynamically for visible window.

Layout structure:
- Left panel: Event list (rows)
- Right panel: Time axis visualization

Controls:
- Time scale control (zoom in/out)
- Time window scroll
- Toggle per event source (enable/disable)
- Grouping mode:
  - By category
  - By source
  - Flat list

Rendering rules:
- Duration events: horizontal bars
- Instant events: vertical markers
- Open-ended events: bar extending to current time if active
- Recurring events: rendered as generated instances within window

Optional visual overlay:
- When zoomed at day-level resolution or wider,
  background shading MAY reflect dominant activity category per day
  (no segment detail shown here).

Actions:
- Click event row → open Event Editor
- Add event → open Event Editor (new)
- Toggle recurrence → open Recurrence Editor
- Delete (soft delete)
- Resize duration (future enhancement; not required in V1)

Transitions:
- Event Editor modal
- Recurrence Editor modal

---

# 5. State: Daily Cycles View

Purpose:
Render daily activity distribution as continuous segments.

Layout:
- Vertical list of dates (rows)
- Each row contains a 24-hour horizontal axis
- Colored segments proportional to (end_at - start_at)

Data source:
- `activity_segments` from active activity_sheet
- Filtered by date range and category filters

Rendering rules:
- Segments sorted by start_at
- Width proportional to duration
- No fixed hourly granularity required
- Midnight crossing not possible (enforced by D2)

Controls:
- Date range selector
- Category filter toggle
- Preset selector (e.g., Sleep-only, Work-only, All)

Interaction:
- Click on any date row → open Day Workspace
- Add Segment button (optional shortcut)

Transitions:
- Day Workspace modal/window

---

# 6. State: Day Workspace (Activity Segments Editor)

Trigger:
- User clicks a date in Daily Cycles view

Purpose:
Provide full editing workspace for all segments of one day.

Layout:
Zone A: Day Visualization
- 24-hour continuous timeline
- Colored segments for that day
- Non-editable graphical preview in V1

Zone B: Activity Segments Table
Columns:
- Category
- Start time
- End time
- Tag (optional)
- Notes
- Metadata indicator (if meta_json exists)

Behavior:
- Each row represents one `activity_segment`
- User may:
  - Add new row
  - Edit cell values
  - Delete (soft delete)
- Quick Timestamp Insert available for time fields
- Paste supported in cells

Validation:
- Start < End
- No midnight crossing allowed
  - If entered, system auto-splits into two rows

Save behavior:
- Online: write immediately to Sheets
- Offline: update local cache and queue mutation

Exit:
- Close returns to Daily Cycles view
- Changes persist immediately

---

# 7. State: Event Editor

Trigger:
- Add event
- Click event in timeline

Fields:
- Title
- Category
- Time Type (duration | instant | open_ended)
- Start timestamp
- End timestamp (if applicable)
- Tag
- Notes
- Recurring toggle

Behavior:
- Quick Timestamp Insert supported
- Validation rules per time_type
- If Recurring toggle enabled → open Recurrence Editor

Save:
- Online: write to source spreadsheet
- Offline: queue mutation

---

# 8. State: Recurrence Editor

Trigger:
- Recurring toggle enabled in Event Editor

Fields:
- Frequency (weekly, monthly)
- Interval
- ByDay
- ByMonthDay
- BySetPos
- Preview of next occurrences (optional but recommended)

Output:
- Generate RRULE string
- Store in `recurrences.rrule`

Validation:
- Only supported RRULE subset allowed

Save:
- Write recurrence row
- Link recurrence_id to event

---

# 9. State: Settings

Tabs:
- Categories
- View Configurations
- Source Management

Categories:
- Add/Edit/Delete
- Color selection
- Sort order

View Configurations:
- Create preset
- Save filters/grouping
- Delete preset

Source Management:
- Show active activity source
- Show active event sources
- Add new event source
- Remove event source (does not delete data, only disconnects)

---

# 10. Sync Status Panel (S3 Decision)

Access:
- Click global sync indicator

Displays per source:
- Source name
- Online/Offline state
- Pending count
- Last sync timestamp
- Sync now button

If conflicts exist:
- Source marked as "Conflict pending"

---

# 11. Conflict Resolution Dialog

Trigger:
- Sync detects conflict

Displays:
- Record summary
- Local values
- Remote values
- Field differences highlighted

Options:
- Keep Local
- Keep Remote
- Manual merge (if enabled)

After resolution:
- Sync resumes for that source

---

# 12. State Transitions Summary

Landing → Active Workspace (after activity source loaded)
Active Workspace → Events Timeline (tab switch)
Active Workspace → Daily Cycles (tab switch)
Daily Cycles → Day Workspace (click date)
Events Timeline → Event Editor
Event Editor → Recurrence Editor
Any state → Sync Panel
Sync Panel → Conflict Dialog (if needed)