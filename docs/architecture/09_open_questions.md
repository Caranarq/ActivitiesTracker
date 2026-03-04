# 09 — Open Questions (V1 + Forward-Looking)

This document lists unresolved decisions, clarified ambiguities, and forward-looking considerations that are intentionally not locked in V1 but must be tracked.

Each item includes:
- Context
- Risk if ignored
- Proposed direction (if any)
- Status

---

# Q1 — Activity Segment Overlap Policy

## Context
Currently, the specification does not explicitly define whether overlapping `activity_segments` within the same day are allowed.

Example:
- 09:00–11:00 Productivity
- 10:30–12:00 Admin

## Risk
- Rendering ambiguity in Daily Cycles view.
- Analytics distortion (double-counted time).
- User confusion.

## Options
- O1: Allow overlaps (render in stacking order; user responsible).
- O2: Disallow overlaps (validation error).
- O3: Allow but visually flag conflicts.

## Proposed Direction
Disallow overlaps in V1 for clarity and integrity.

## Status
OPEN

---

# Q2 — Local Cache Storage Format

## Context
Local cache must be durable and atomic, but format is unspecified.

## Options
- SQLite database
- Embedded lightweight DB
- Structured JSON files
- Other

## Risk
- Corruption on crash
- Complex sync behavior
- Poor performance at 50k+ rows

## Proposed Direction
SQLite recommended for transactional safety.

## Status
OPEN

---

# Q3 — Local Cache Encryption

## Context
OAuth tokens must be secured.
Local dataset may contain sensitive personal activity data.

## Question
Should the local cache be encrypted at rest?

## Tradeoff
- Encryption increases complexity and key management.
- Not encrypting increases risk if machine compromised.

## Status
OPEN (security-level decision)

---

# Q4 — Sync Frequency Policy

## Context
Sync triggers are defined (manual + reconnect).
Periodic background sync interval not locked.

## Questions
- Should sync auto-run every N minutes while online?
- Should large queues batch in chunks?

## Risk
- Excessive API calls
- Stale data if too infrequent

## Status
OPEN

---

# Q5 — Large Dataset Handling Strategy

## Context
Target supports ~50k rows per source.
Google Sheets API performance can degrade with large spreadsheets.

## Questions
- Full reload on each open?
- Incremental fetch based on updated_at?
- Pagination strategy?

## Status
OPEN (likely addressed during implementation planning)

---

# Q6 — Column Preservation Policy

## Context
Spec states the app should preserve unknown columns where feasible.

## Questions
- Is schema strictly app-owned?
- Should unknown columns be retained on update?
- Should strict schema enforcement be required?

## Risk
- Breaking user-customized spreadsheets
- Overwriting manual columns

## Status
OPEN

---

# Q7 — Recurrence Preview in UI

## Context
Recurrence Editor mentions optional preview of next occurrences.

## Question
Is preview required in V1 or optional enhancement?

## Status
OPEN

---

# Q8 — Activity Segment Metadata Usage

## Context
`meta_json` field exists but no specific usage defined.

## Future Possibilities
- Sleep quality metrics
- Health metrics
- Device import flags
- Derived metrics

## Status
OPEN (reserved for V2+)

---

# Q9 — Multi-Source Event Conflict Resolution

## Context
Conflicts are per source.
Multiple event sources may conflict independently.

## Question
Should conflict resolution UI allow:
- Batch resolution?
- One-by-one only?

## Status
OPEN

---

# Q10 — Timezone Migration Scenario

## Context
Records store `tz_capture`.
Future versions may allow display timezone override.

## Question
If user relocates permanently:
- Should existing records remain fixed to capture timezone?
- Should a migration tool exist?

## Status
OPEN (future design consideration)

---

# Q11 — Hard Delete Capability (Admin/Debug)

## Context
Spec enforces soft deletes only.

## Question
Should there be a hidden admin tool to permanently purge deleted records?

## Risk
Data bloat over years.

## Status
OPEN (low priority)

---

# Q12 — Analytics and Aggregations (Future)

## Context
Model supports time-based analysis.

## Possible Extensions
- Weekly category totals
- Monthly time allocation
- Planned vs actual comparison (events vs segments)
- Attention per tag

## Status
OUT OF SCOPE FOR V1