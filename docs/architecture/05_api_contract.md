# 05 — API Contract (Sheets Sync + Event Sources)

This document defines the contractual behavior between the Windows client and:
- Google Sheets (V1 canonical store for the Activity Sheet and for Event sources of type `google_sheets_events`)
- Future event sources (e.g., Google Calendar), as an interface boundary

No implementation details are specified here—only inputs/outputs, invariants, and error handling.

---

## 1. Actors and Components

- Client: Portable Windows 10 desktop application.
- Canonical Stores (Google Sheets):
  - Exactly one Activity Sheet source (source_type: `activity_sheet`)
  - One or more Event Sheet sources (source_type: `google_sheets_events`)
- Local Store:
  - Per-source Local Cache
  - Per-source Pending Changes Queue
  - Source Registry + Recents Index
- Auth Provider: Google OAuth.

---

## 2. Authentication Contract (Google OAuth)

### 2.1 Requirements
- Client MUST use interactive OAuth login for Google APIs.
- Client MUST request the minimum scopes required to read/write the configured spreadsheets.
- Tokens MUST be stored locally using OS-appropriate secure storage (details finalized in NFR).

### 2.2 Auth Failure Handling
- If OAuth token is missing/expired and user is online:
  - Client prompts user to re-authenticate.
- If OAuth token is missing/expired and user is offline:
  - Client remains usable with Local Cache (read/write offline),
  - Sync is blocked until re-authentication succeeds.

---

## 3. Source Types and Source Registry Contract

### 3.1 Source Registry Object (local)
Each configured source is represented locally as:

```json
{
  "source_id": "uuid",
  "source_type": "activity_sheet | google_sheets_events | google_calendar (future)",
  "display_name": "string",
  "spreadsheet_id": "string (for sheets sources)",
  "is_active": true,
  "last_opened_at": "datetime"
}
````

Rules:

* Exactly one `activity_sheet` source is active.
* Multiple `google_sheets_events` sources may be active concurrently.

### 3.2 Required Tabs by Source Type (V1)

Activity Sheet (`activity_sheet`) MUST contain:

* `categories`
* `view_configs`
* `activity_segments`

Event Sheet (`google_sheets_events`) MUST contain:

* `events`
* `recurrences` (required if recurrence is used; otherwise MAY exist)

---

## 4. Google Sheets Schema and Validation Contract

### 4.1 Tab Discovery and Schema Validation

On source connect (online), the client MUST:

1. List sheets/tabs in the spreadsheet.
2. Validate required tabs exist for the source type.
3. For each required tab, validate:

   * Header row exists
   * Required columns exist (as defined in `04_data_model.md`)
4. If tabs/columns are missing:

   * If initiated from “Create Dataset”: client initializes schema.
   * If initiated from “Open Dataset”: client shows actionable schema error.

### 4.2 Schema Initialization

Schema initialization contract (V1):

* Client creates required tabs with exact names and header columns.
* Client MAY add optional columns but MUST NOT remove/rename required columns.
* Initialization must be safe to re-run (idempotent or safely repeatable).

---

## 5. Canonical Row Identity and Mutation Contract

### 5.1 Row Identity Mapping

Canonical identity is by `id` column (UUID).

* The `id` column MUST be present.
* Client MUST treat `id` as immutable.
* Updates MUST target rows by matching `id`.

### 5.2 Soft Delete Policy

Hard deletion is not permitted in V1.
Soft delete means:

* Set `is_deleted=true`
* Set `deleted_at` (timestamp)
* Update `updated_at`

### 5.3 Updated-at Policy

* Client MUST set `updated_at` on every insert/update/delete.
* Conflict detection relies on `updated_at` and local baselines.

---

## 6. Read Contract

### 6.1 Load Behavior (V1)

Minimum required behavior:

* On first open: full load each required tab into local cache.
* On subsequent opens: client MAY do full load or incremental refresh; correctness is required either way.

### 6.2 Row Filtering

* Client loads soft-deleted rows into cache.
* Default UI views filter out `is_deleted=true`.

---

## 7. Write Contract (Online and Offline)

### 7.1 Operations

Supported operations per tab:

* Insert: append a row with new UUID `id`
* Update: locate row by `id` and update fields
* Soft delete: update row by `id` setting soft-delete fields

### 7.2 Online Writes

When online and authenticated:

* Client writes to remote Sheet.
* Client updates local cache to reflect the remote-applied state.

### 7.3 Offline Writes

When offline or unauthenticated:

* Client writes to local cache immediately.
* Client enqueues a Pending Changes item (section 8).
* UI reflects the local cache state immediately.

### 7.4 Update Row Not Found

If an update/delete cannot find a remote row by `id`:

* Client marks the queue item as `failed`.
* Client prompts user with recovery options (retry after reload; or treat as new insert if user approves).

---

## 8. Offline-First Sync Contract

### 8.1 Local Cache Contract

* Client maintains a per-source local cache of canonical tabs.
* Local cache is the read/write target while offline.

### 8.2 Pending Change Queue Contract

Each queued mutation item:

```json
{
  "queue_id": "uuid",
  "source_id": "uuid",
  "tab_name": "events | recurrences | activity_segments | categories | view_configs",
  "operation": "insert | update | soft_delete",
  "record_id": "uuid",
  "payload_json": "{...}",
  "created_at": "datetime",
  "status": "pending | applied | blocked_conflict | failed",
  "error_message": "string|null"
}
```

Rules:

* Queue is ordered per `source_id` by `created_at`.
* Sync processes each `source_id` independently.
* UI may present combined sync state across sources (global pending count + per-source details).

### 8.3 Sync Triggers (V1)

* Automatic: when transitioning Offline -> Online.
* Manual: user clicks “Sync Now”.

Periodic background sync interval is TBD (handled in NFR/open questions).

### 8.4 Sync Algorithm (per source)

For each queued item in order:

1. Fetch remote row by `record_id` (if applicable).
2. Detect conflict (section 9).
3. If no conflict:

   * Apply mutation to remote spreadsheet.
   * Update local cache to match remote outcome.
   * Mark queue item `applied`.
4. If conflict:

   * Mark queue item `blocked_conflict`.
   * Pause sync for that source.
   * Prompt user to resolve conflict (section 9.3).

Resumability:

* If app closes during sync, queue persists and continues next run.

---

## 9. Conflict Detection and Resolution Contract

### 9.1 Baseline Tracking

Client MUST store, per cached row:

* last known remote `updated_at` at time of last successful sync/refresh (baseline).

### 9.2 Conflict Condition (V1)

A conflict exists when:

* There is a local pending mutation for `record_id`, AND
* The remote row exists, AND
* Remote `updated_at` > local baseline `updated_at` for that record.

### 9.3 Conflict Resolution UX Contract

When conflict occurs, client MUST show:

* Record summary
* Local version vs remote version
* Timestamps: last local edit time and remote `updated_at`

User action outcomes:

* Keep Local: overwrite remote with local payload; update remote `updated_at`.
* Keep Remote: discard local queued mutation for that record; update local cache to remote.
* Manual Merge: optional in V1; if implemented, results in a merged payload applied as “Keep Local merged”.

Hard requirement:

* Client MUST NOT silently overwrite on conflict.
* Client MUST log the user choice locally.

---

## 10. Recurrence Contract (RRULE-like subset) — V1

Recurrences are stored as `recurrences.rrule` strings.
The client MUST support generating occurrences for display windows and MUST reject or flag unsupported rules safely.

### 10.1 Required Supported Capabilities

* Monthly, last day of month:

  * `FREQ=MONTHLY;BYMONTHDAY=-1`
* Monthly, nth weekday of month:

  * Second Tuesday:

    * `FREQ=MONTHLY;BYDAY=TU;BYSETPOS=2`
  * Last Friday:

    * `FREQ=MONTHLY;BYDAY=FR;BYSETPOS=-1`
* Weekly interval:

  * Every 2 weeks on Monday:

    * `FREQ=WEEKLY;INTERVAL=2;BYDAY=MO`

Anchor fields:

* `start_anchor_at` is required and defines the baseline occurrence.
* `tz_capture` defines timezone interpretation.

### 10.2 Window-Limited Expansion

Given:

* a recurrence record
* a requested time window `[window_start, window_end]`

Client returns a list of occurrences intersecting the window.
Occurrence generation MUST be deterministic and stable.

### 10.3 Unsupported Rule Handling

If an RRULE is outside the supported subset:

* Client MUST mark the recurring event as “Unsupported recurrence rule” in UI.
* Client MUST NOT crash.
* Client SHOULD allow editing the recurrence into a supported pattern.

---

## 11. Multi-Source Events Contract (V1)

### 11.1 Overlay Model

Events Timeline view displays events from all enabled active event sources.
User can:

* enable/disable each event source via toggle
* group rows by source or by category

### 11.2 Independence

* Each event source has independent cache, queue, sync, and conflict resolution.
* Conflict in one source pauses sync only for that source; other sources may continue.

---

## 12. Future Event Source Interface (Google Calendar) — Boundary Only

This section defines a future-compatible interface so the client can treat event sources uniformly.

### 12.1 Provider Interface (conceptual)

Each event provider should implement:

* `list_events(window_start, window_end, filters) -> EventInstance[]`
* `get_event(event_id) -> EventRecord`
* `create_event(payload) -> EventRecord` (optional)
* `update_event(event_id, payload) -> EventRecord` (optional)
* `delete_event(event_id) -> void` (optional)

Where:

* `EventRecord` maps to canonical `events` fields where possible.
* `EventInstance` represents generated occurrences for recurring events.

### 12.2 Canonicality Stance (V1)

* V1 canonical store remains Google Sheets.
* Future sources like Google Calendar may be overlays or writable sources; not decided in V1.

---

## 13. Error Handling Contract (V1)

Client MUST classify errors:

* Auth errors (OAuth)
* Network errors (offline/transient)
* Schema errors (missing tabs/columns)
* Validation errors (bad timestamps, start/end logic)
* Sync errors (row not found, conflicts, rate limits)

Client behavior:

* Never lose local edits silently.
* Failed remote writes must keep queue items retryable (or failed with a clear message).
* Provide user a way to retry sync.

```

---

Siguiente: **01_prd.md** (también estaba en 0KB). Ahí amarramos alcance V1, prioridades, criterios de éxito y no-goals de producto en formato PRD.
