# 08 — Test Plan (V1)

This document defines acceptance-level test cases for V1, focused on correctness, data integrity, offline-first behavior, multi-source events, recurrence, and timezone handling.

Test cases are written to be executable manually or convertible into automated tests later.

---

## 1. Test Data Setup

### 1.1 Test Sources
- One Activity Sheet source (Google Spreadsheet) with tabs:
  - `categories`, `view_configs`, `activity_segments`
- Two Event Sheet sources (Google Spreadsheets) with tabs:
  - `events`, `recurrences`

### 1.2 Category Fixtures
Create categories in Activity Sheet:
- Sleep
- Productivity
- Health
- Procrastination
- Admin

Ensure each has a distinct `color_hex`.

### 1.3 Timezone Fixture
Primary timezone: `America/Monterrey`

---

## 2. Launch, Sources, and Recents

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| L1 | Landing shows controls | Launch app | Landing shows New, Open, Recents sections |
| L2 | Recents persist | Open a source, close app, reopen | Recent entry exists with last_opened_at updated |
| L3 | Offline availability shown | Open source once (to create cache), go offline, relaunch | Recent shows “Available Offline” |
| L4 | Open cached while offline | Go offline, open recent with cache | App opens Active Workspace using local cache |
| L5 | Open non-cached while offline | Ensure no cache exists, go offline, attempt open | App blocks with clear error; no crash |

---

## 3. Schema Validation and Initialization

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| S1 | Create schema success | New Activity Source, point to empty spreadsheet | Required tabs + headers created |
| S2 | Missing tab error | Remove `activity_segments` tab, attempt open | Clear schema error; no partial load |
| S3 | Missing column error | Remove `updated_at` column from `events`, open event source | Clear schema error; no crash |
| S4 | Safe re-init | Run schema init on already-initialized sheet | No destructive changes; idempotent behavior |

---

## 4. Categories and Configuration

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| C1 | Add category | Settings → Categories → Add | Category appears in lists; persists after restart |
| C2 | Edit color | Change category color | Daily Cycles reflects new color |
| C3 | Delete blocked if referenced | Create segment referencing category, then delete category | Delete prevented with clear message |
| C4 | View preset saved | Create view preset with filters | Preset persists and re-applies correctly |

---

## 5. Activity Segments (Daily Cycles + Day Workspace)

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| A1 | Daily cycles renders segments | Create segments for a day | Row renders continuous bars proportional to duration |
| A2 | Open Day Workspace | Click a date row | Day Workspace opens with visualization + table for that day |
| A3 | Add row in workspace | Add new segment row, save | Appears immediately in table and daily row rendering |
| A4 | Edit cells | Modify start/end/category in table | Validation applies; view updates immediately |
| A5 | Paste into timestamp fields | Paste ISO datetime values | Fields accept; record saves; correct parsing |
| A6 | Quick timestamp insert | Use quick insert on start/end fields | Current timestamp inserted; save succeeds |
| A7 | Soft delete segment | Delete a segment | `is_deleted=true`; segment disappears from default views |
| A8 | D2 midnight split | Enter 22:00–02:00 segment | System splits into two rows across dates; durations preserved |
| A9 | Segment overlap tolerance | Create overlapping segments in same day | System behavior is consistent (either allowed with warning or blocked; define final rule in UI states if needed) |

Note: Overlap policy is not explicitly decided. If V1 allows overlaps, tests assert it is rendered deterministically. If V1 blocks overlaps, tests assert validation error.

---

## 6. Events (Multi-source Timeline + Editor)

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| E1 | Multi-source render | Load two event sources | Timeline shows events from both sources |
| E2 | Source toggles | Disable one source | Events from that source disappear; others remain |
| E3 | Group by source | Enable grouping by source | Rows reorganize by source lanes |
| E4 | Group by category | Enable grouping by category | Rows reorganize by category |
| E5 | Add duration event | Create duration event | Bar appears with correct placement and length |
| E6 | Add instant event | Create instant milestone | Marker appears at timestamp |
| E7 | Add open-ended event | Create open-ended event | Renders as open-ended (end null) |
| E8 | Close open-ended event | Edit event to set end | Becomes duration; timeline updates |
| E9 | Tag stored | Set tag and save | Tag persists; no coupling to activity_segments (X1) |
| E10 | Soft delete event | Delete event | `is_deleted=true`; removed from default timeline |

---

## 7. Recurrence (RRULE Subset + Expansion)

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| R1 | Monthly last day | Create RRULE `FREQ=MONTHLY;BYMONTHDAY=-1` | Instances appear on last day of each visible month |
| R2 | Monthly nth weekday | Create RRULE `FREQ=MONTHLY;BYDAY=TU;BYSETPOS=2` | Instances appear on 2nd Tuesday |
| R3 | Monthly last weekday | Create RRULE `FREQ=MONTHLY;BYDAY=FR;BYSETPOS=-1` | Instances appear on last Friday |
| R4 | Weekly interval | Create RRULE `FREQ=WEEKLY;INTERVAL=2;BYDAY=MO` | Instances appear every 2 weeks on Monday |
| R5 | Window-limited expansion | Set timeline window to 1 month | Only occurrences within window generated/rendered |
| R6 | Unsupported RRULE handling | Enter unsupported RRULE (e.g., BYHOUR) | UI marks unsupported; app does not crash; edit allowed |
| R7 | Toggle recurrence off | Disable recurring toggle | recurrence_id removed/ignored; instances disappear |

---

## 8. Offline-first Behavior (Cache + Queue)

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| O1 | Offline indicator | Disconnect network | UI shows Offline state |
| O2 | Offline create activity segment | Offline: add segment | Writes to cache; pending count increments |
| O3 | Offline create event | Offline: add event | Writes to cache; pending increments for that source |
| O4 | Persist queue across restart | Offline edits, close app, reopen | Pending queue preserved; data visible locally |
| O5 | Auto sync on reconnect | Reconnect network | Sync starts; pending decreases to zero if no conflicts |
| O6 | Manual sync | Online with pending: click Sync Now | Sync executes; statuses update per source |

---

## 9. Conflict Resolution

Setup:
- Ensure a record exists locally and remotely.
- Create a pending local edit while offline.
- Separately modify the same remote record (simulate by editing sheet directly) with newer `updated_at`.

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| F1 | Conflict detected | Reconnect and sync | Sync pauses for source; conflict dialog appears |
| F2 | Keep Local | Choose Keep Local | Remote overwritten with local payload; sync resumes |
| F3 | Keep Remote | Choose Keep Remote | Local pending discarded for record; local cache updated; sync resumes |
| F4 | Conflict logging | Resolve conflict | Local log entry created with user choice |
| F5 | Multi-source isolation | Conflict in one source | Only that source pauses; other sources may continue syncing |

---

## 10. Timezone Handling

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| T1 | tz_capture stored on insert | Create event/segment | `tz_capture` populated |
| T2 | Display defaults to tz_capture | Create record with tz_capture | UI displays time aligned to tz_capture |
| T3 | Midnight split respects tz_capture | Segment crossing midnight | Split occurs at midnight in tz_capture timezone |
| T4 | Future-proofing | Edit tz_capture (if allowed) | Behavior defined; if not allowed, edit blocked |

---

## 11. Data Integrity and Recovery

| ID | Test | Steps | Expected |
|----|------|-------|----------|
| D1 | Missing row on update | Delete row in sheet, then sync update | Queue item fails with clear error; no data loss locally |
| D2 | Duplicate id prevention | Attempt insert with existing id | Operation blocked or treated idempotently |
| D3 | Soft delete respected | Mark is_deleted true | Record excluded from default views |
| D4 | Schema drift tolerance | Add extra columns manually | App preserves unknown columns where feasible |

---

## 12. Acceptance Criteria for V1 Release

V1 is acceptable if:
- All tests in sections 2–10 pass for at least one activity source and two event sources.
- No test reveals silent data loss or silent overwrites.
- Offline edits persist and sync reliably after reconnect.
- Recurrence renders correctly for required subset.
- Daily Cycles view matches continuous-time behavior (no forced hourly granularity).