# 07 — Non-Functional Requirements (V1)

This document defines system-level qualities and constraints that are not feature-specific, but mandatory for correct operation and future extensibility.

---

# 1. Performance Requirements

## 1.1 Dataset Size Targets (V1 Assumptions)

The system is designed for personal-scale datasets.

Expected upper bounds for V1:

Activity Sheet:
- activity_segments: up to 50,000 rows
- categories: < 200 rows
- view_configs: < 200 rows

Per Event Source:
- events: up to 50,000 rows
- recurrences: < 5,000 rows

The system MUST remain responsive under these conditions.

## 1.2 UI Responsiveness

- View switches (Events ↔ Daily Cycles) must render within 500 ms for datasets under target size.
- Opening Day Workspace must render within 300 ms.
- Timeline zoom operations must not freeze UI.

## 1.3 Recurrence Expansion Performance

Recurrence generation must:
- Be limited to visible time window.
- Not pre-generate unbounded future occurrences.
- Handle at least 5,000 recurrence rules without blocking UI.

---

# 2. Offline Durability

## 2.1 Local Cache Integrity

- Local cache MUST persist across app restarts.
- Pending changes queue MUST persist across restarts.
- No local edits may be lost due to unexpected shutdown.

## 2.2 Write Safety

- Writes to local cache MUST be atomic.
- Partial writes MUST not corrupt cache.
- Queue state transitions MUST be transactional.

---

# 3. Sync Reliability

## 3.1 Conflict Safety

- System MUST never silently overwrite remote changes.
- All conflicts require explicit user decision.

## 3.2 Retry Behavior

- Network failures MUST NOT discard queue items.
- Failed operations must remain retryable.
- Rate limit errors must trigger exponential backoff.

## 3.3 Idempotency

- Insert/update operations must be idempotent with respect to record `id`.
- Replaying the same queue item twice must not corrupt data.

---

# 4. Security Requirements

## 4.1 OAuth Token Storage

- OAuth tokens MUST be stored securely using OS-level secure storage if available.
- Tokens MUST NOT be stored in plain text configuration files.

## 4.2 Local Cache Protection

- Local cache must be stored in user-scoped directory.
- Local files must not require admin privileges.
- No external network transmission except Google APIs.

## 4.3 Data Privacy Boundary

- Data must never be transmitted to third-party services other than:
  - Google Sheets API
  - Google OAuth endpoints
- No telemetry or analytics in V1.

---

# 5. Timezone Handling

## 5.1 Capture Integrity

- All time-based records MUST store `tz_capture`.
- `start_at` and `end_at` are interpreted relative to `tz_capture`.

## 5.2 Rendering Rules

- By default, display times in:
  - `tz_capture`
- Future versions may support display timezone override.

## 5.3 Midnight Boundary Rule

For activity_segments:
- System MUST split segments crossing midnight at 00:00 local time of `tz_capture`.
- Split must preserve total duration exactly.

---

# 6. Data Integrity

## 6.1 Required Columns

- All canonical tabs must contain required columns defined in 04_data_model.md.
- Client must validate schema before operating.

## 6.2 Soft Deletes

- Soft-deleted rows must not appear in default views.
- Hard deletion is not permitted in V1.

## 6.3 Referential Integrity

- category_id must reference existing non-deleted category.
- recurrence_id must reference existing non-deleted recurrence.

Client must prevent save if referential integrity would break.

---

# 7. Extensibility Constraints

## 7.1 Source Extensibility

The system must support future event source types (e.g., google_calendar) without:
- Refactoring core UI states
- Changing canonical event rendering logic

## 7.2 View Extensibility

New view types must be addable by:
- Adding new view_type in view_configs
- Adding corresponding UI state

Without modifying canonical data schema.

---

# 8. Logging and Observability

## 8.1 Local Logs

The client SHOULD log:
- Sync attempts
- Sync failures
- Conflict resolutions
- Schema validation failures

Logs must be:
- Local only
- Rotated to prevent unbounded growth

---

# 9. Usability Guarantees

## 9.1 No Data Loss on User Error

- User must confirm destructive actions (delete source, delete category).
- Deleting category must be blocked if referenced.

## 9.2 Input Flexibility

Timestamp fields must accept:
- Full ISO datetime
- Date-only (auto-complete time to 00:00)
- Time-only (auto-fill date contextually)
- Quick Insert current timestamp

---

# 10. Failure Modes

The system must degrade gracefully in:

- Network loss
- OAuth expiration
- Partial sync
- Schema mismatch
- Recurrence rule parsing failure

The system must:
- Inform user clearly
- Preserve local data
- Remain operable offline

---

# 11. Stack and Packaging Constraints (V1)

- The V1 reference implementation stack is:
  - C# / .NET as implementation language/runtime
  - WPF (.NET) for UI
  - SQLite for local cache and sync queue
  - Google OAuth + Google Sheets APIs via official .NET libraries
- The deliverable MUST support a portable, self-contained distribution:
  - Running on Windows 10 without requiring admin privileges
  - No external runtime installation required by the user for execution
