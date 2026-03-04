# 03 — Happy Path Flows (V1)

This document defines the step-by-step workflows the application must support in V1.
Scope includes: dataset creation/opening, activity/event logging (duration/instant/open-ended), activity segment logging for daily cycles, view usage, offline-first behavior, sync, and user-driven conflict resolution.

## Terminology

- Dataset: A paired configuration pointing to two Google Sheets (or two tabs/worksheets under one spreadsheet, TBD later):
  - Activity Sheet
  - Events sheet
- Local Cache: Local stored copy of the dataset used for offline work.
- Pending Changes Queue: Local list of unsynced mutations to be applied to Sheets when online.
- Timestamp Field: Any input that accepts date/time or datetime strings.
- Quick Timestamp Insert: A UI action that injects “now” into the currently focused Timestamp Field.

## Global UX Rules (apply across flows)

1. The app MUST always show connectivity state (Online / Offline).
2. The app MUST show pending change count when > 0.
3. The app MUST allow working against Local Cache when Offline.
4. The app MUST support paste into input fields.
5. The app MUST support Quick Timestamp Insert for any Timestamp Field.
6. The app MUST store the timezone-at-capture per record when capturing timestamps.

---

## Flow F0 — Launch / Landing (No dataset open)

Trigger:
- App starts OR user closes current dataset.

Steps:
1. App opens Landing Window.
2. Landing Window shows:
   - Button: New (Create Dataset)
   - Button: Open (Open Existing Dataset)
   - Section: Recent Datasets (list)
3. Recent Datasets list items show at minimum:
   - Dataset name (user-defined or derived)
   - Last opened datetime
   - Status indicator: “Available offline” if local cache exists
4. User action options:
   - Select a recent dataset -> proceed to F2 (Open Recent Dataset)
   - Click New -> proceed to F1 (Create Dataset)
   - Click Open -> proceed to F3 (Open Dataset by Link/ID)

Acceptance Criteria:
- Landing Window is reachable at any time.
- Recent datasets are visible without network access.
- User can open a cached dataset while offline.

---

## Flow F1 — Create Dataset (New)

Trigger:
- User clicks “New” on Landing Window.

Preconditions:
- User is online OR user can authenticate later (app should guide; creation requires online).

Steps:
1. App prompts for Google authentication (OAuth) if not already authenticated.
2. App asks user for:
   - Activity Sheet link OR a target Spreadsheet link where the app can create required tabs (exact mechanics TBD later).
   - Events Sheet link OR create target.
   - Dataset display name (for Recent Datasets list).
3. User confirms creation.
4. App initializes required schema structures in the target Sheets (tabs/headers/config tables; details in Data Model file).
5. App creates Local Cache for the dataset.
6. App sets this dataset as the Active Dataset.
7. App navigates to default view (Events Overview Timeline).

Postconditions:
- Sheets contain initialized schema.
- Active Dataset is available in Recent Datasets.
- Local Cache exists and is marked “Available offline.”

Acceptance Criteria:
- Creation is idempotent or fails safely (no partial corrupted schema).
- After creation, app can operate without requiring immediate re-authentication.

---

## Flow F2 — Open Recent Dataset

Trigger:
- User selects a dataset from Recent Datasets.

Steps:
1. App checks connectivity state.
2. If Online:
   - App authenticates (OAuth) if needed.
   - App attempts to load latest dataset from Sheets.
   - App updates Local Cache.
3. If Offline:
   - App loads dataset from Local Cache.
4. App sets dataset as Active Dataset.
5. App navigates to last-used view for this dataset (or default Events Overview).

Acceptance Criteria:
- Offline open works if cache exists.
- If cache does not exist and offline, app shows a clear error and returns to Landing Window.

---

## Flow F3 — Open Dataset by Link/ID

Trigger:
- User clicks “Open” on Landing Window.

Steps:
1. App prompts user to provide:
   - Activity Sheet link
   - Events Sheet link
   - Dataset display name (optional but recommended)
2. App checks connectivity state:
   - If Offline: app informs user it cannot validate links; offers to save as “pending” but cannot open until online (optional). Returns to Landing Window.
   - If Online: proceed.
3. App authenticates (OAuth) if needed.
4. App validates that required schema exists (or offers to initialize if blank/compatible; exact rules TBD).
5. App downloads dataset into Local Cache.
6. App adds dataset to Recent Datasets.
7. App sets dataset as Active Dataset and opens default view.

Acceptance Criteria:
- Schema validation produces human-readable errors.
- After successful open, dataset is available offline.

---

## Flow F4 — Add Duration Activity/Event (Start + End)

Trigger:
- User clicks “Add” in Events view OR Activity view.

Steps:
1. App opens a Record Editor form.
2. User enters:
   - Category/Type
   - Start timestamp (supports Quick Timestamp Insert)
   - End timestamp (supports Quick Timestamp Insert)
   - Optional tag / notes
3. User clicks Save.
4. App validates:
   - Start <= End
   - Required fields present
5. App writes change:
   - If Online: write to Sheets AND update Local Cache.
   - If Offline: write to Local Cache and add mutation to Pending Changes Queue.
6. App updates current view immediately.

Acceptance Criteria:
- Saving while offline is allowed and clearly queued.
- View updates without requiring page refresh.

---

## Flow F5 — Add Instant Event (Milestone)

Trigger:
- User selects “Instant” or “Milestone” record type.

Steps:
1. Record Editor shows a single timestamp field.
2. User enters timestamp (Quick Timestamp Insert supported).
3. Save -> same write behavior as F4.

Acceptance Criteria:
- Instant events render appropriately in timeline (marker/dot) and do not require an end time.

---

## Flow F6 — Start Open-Ended Activity

Trigger:
- User selects “Open-ended” record type OR clicks “Start Activity” shortcut (if provided in UI).

Steps:
1. App creates a new record with:
   - Start timestamp default = now (user can edit; Quick Timestamp Insert supported)
   - End timestamp = null
2. User enters:
   - Category/Type
   - Optional tag (recommended for tracking attention/effort in daily cycles)
   - Notes (optional)
3. User clicks Save.
4. App writes change (online/offline rules as in F4).
5. Active open-ended activities appear in a visible “Active” list (UI surface TBD, but behavior required).
6. No `activity_segments` record is auto-created from this event. If desired, the user must log the corresponding activity segment manually (see F9).

Acceptance Criteria:
- Open-ended activities are clearly distinguishable (no end time).
- User can start open-ended activity offline.

---

## Flow F7 — Close Open-Ended Activity

Trigger:
- User selects an active open-ended activity and chooses “Close”.

Steps:
1. App opens editor focused on End timestamp.
2. End timestamp default = now (Quick Timestamp Insert supported).
3. User confirms Close.
4. App validates End >= Start.
5. App writes update (online/offline rules).
6. Activity disappears from “Active” list and becomes a normal duration record.
7. Closing an open-ended event does not auto-generate or update any `activity_segments` rows.

Acceptance Criteria:
- Close operation works offline and queues mutation.
- Closing updates the timeline immediately.

---

## Flow F8 — Create / Edit Recurring Event (Optional Toggle)

Trigger:
- User creates a new event OR edits an existing event and toggles “Recurring”.

Steps (Create):
1. User creates a normal event (instant or duration).
2. User toggles “Recurring: On”.
3. App shows recurrence rule editor fields, supporting:
   - Monthly patterns including “Nth weekday of month” and “last day of month”
   - Other common rules (weekly interval, etc.)
4. User saves.
5. App stores a recurrence rule (rule-based model) associated with the event template.

Steps (Edit):
1. User opens an existing event.
2. User toggles “Recurring” on/off.
   - If off: recurrence rule is removed/disabled.
   - If on: recurrence rule editor shown.
3. Save -> apply changes.

Rendering behavior:
- App generates occurrences dynamically for visualization windows (e.g., showing the next N occurrences or those within date range).

Acceptance Criteria:
- Recurrence creation is optional, not required.
- “Nth weekday” and “last day” monthly rules are supported.

---

## Flow F9 — Log Activity Segment (Manual Entry)

Trigger:
- User is in Daily Cycles view and clicks “Add Segment” (or uses a preset such as Sleep-only).

Steps:
1. App opens Segment Editor:
   - Date (default = selected row’s date)
   - Start timestamp
   - End timestamp
   - Label/Category (configurable list)
2. Quick Timestamp Insert supported for start/end.
3. Save validates:
   - Start < End
   - Segment is within logical bounds (exact day-boundary rules TBD later)
4. App writes change (online/offline rules).
5. Daily bar for that date re-renders with updated segments/colors.

Acceptance Criteria:
- Manual segment creation works offline.
- Multiple segments per day are supported.

---

## Flow F10 — Offline Work and Sync

Trigger:
- App transitions Offline -> Online OR user clicks “Sync Now”.

Steps:
1. App detects Online state.
2. App displays “Syncing…” status and current pending count.
3. App replays Pending Changes Queue against Sheets in order.
4. On successful sync of an item:
   - Remove item from queue
   - Ensure Local Cache reflects post-sync state
5. When queue is empty:
   - UI shows “All changes synced”

Acceptance Criteria:
- Sync is resumable (if interrupted, remaining queue persists).
- User always sees pending count and sync state.

---

## Flow F11 — Conflict Detected During Sync (Ask User)

Trigger:
- During F10, app detects that a remote record changed since last local sync AND local edits exist.

Steps:
1. App pauses sync for the conflicting item.
2. App shows Conflict Resolution dialog with:
   - Record identifier (human-friendly summary)
   - Local version fields
   - Remote version fields
   - Timestamp of last local edit and last remote edit (if available)
3. User chooses one action (exact options to be finalized later, but must include):
   - Keep Local (overwrite remote)
   - Keep Remote (discard local change for this record)
   - Manual Merge (field-by-field) (optional for V1; if excluded, state explicitly)
4. App applies user choice:
   - Updates Sheets and Local Cache accordingly
   - Removes or updates queue item
5. App resumes sync.

Acceptance Criteria:
- Sync does not silently overwrite on conflict.
- User choice is explicit and logged (at least locally).

---

## Flow F12 — Configure Categories and Colors (Basic)

Trigger:
- User opens Settings/Configuration.

Steps:
1. User views list of categories/labels.
2. User can add/edit/delete a category.
3. User can set color mapping per category.
4. Save persists configuration to Sheets (and cache).

Acceptance Criteria:
- Configuration changes are reflected immediately in views.
- Configuration is available offline if cached.
