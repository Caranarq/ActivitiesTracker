# 01 — Product Requirements Document (PRD)

## 1. Product Overview

### 1.1 Product Name (Working Title)
Time-Tracked Life Framework (Windows)

### 1.2 Product Type
Portable Windows 10 desktop application.

### 1.3 Core Purpose
Provide a structured, extensible framework for tracking “things that happen in time” across two domains:

- Planned/external events (appointments, payments, releases, milestones).
- Actual activity segments (what was done during each portion of a day).

The system uses Google Sheets as the canonical backend while operating offline-first with local caching and controlled synchronization.

---

## 2. Problem Statement

Existing scheduling tools (e.g., CPM/Gantt software):

- Are overly complex for personal life tracking.
- Focus on project logic rather than real-life time distribution.
- Do not support flexible daily-cycle visualization.
- Do not support easy integration with simple, user-owned data stores.

The product solves:

- Continuous tracking of daily time usage.
- Tracking of recurring external events.
- Overlay of multiple event sources.
- Offline-first interaction with later synchronization.
- Extensibility without reworking the data model.

---

## 3. Target User

### Primary User
- Single advanced individual user.
- Technically literate.
- Comfortable with structured data.
- Uses Google Sheets.

### Usage Pattern
- Daily interaction.
- Frequent manual entry.
- Occasional editing of past records.
- Periodic addition of recurring events.

Future expansion:
- Optional read/share access to other users’ datasets (not in V1).

---

## 4. Scope Definition (V1)

### 4.1 In Scope

Activity Domain:
- Create/edit/delete activity segments.
- Continuous-time daily visualization.
- Category-based coloring.
- Midnight boundary enforcement.
- Tagging for optional correlation.

Event Domain:
- Multiple event sheet sources.
- Duration, instant, and open-ended events.
- Optional recurrence (RRULE subset).
- Timeline view with zoom and grouping.
- Source toggling.

Infrastructure:
- Offline-first operation.
- Per-source local cache.
- Per-source pending change queue.
- OAuth authentication.
- Conflict detection and interactive resolution.
- Schema validation and initialization.

Configuration:
- Category management.
- View presets.
- Source management.

---

## 5. Out of Scope (V1)

The following are explicitly excluded:

- CPM logic (FS/SS/FF/SF relationships).
- Critical path calculation.
- Automatic device/wearable sleep import.
- Automatic online ingestion of release schedules.
- Hard deletes.
- Analytics dashboards (aggregations, weekly/monthly reports).
- Plugin runtime system.
- Multi-user permission system.

---

## 6. User Value Propositions

### 6.1 For Daily Awareness
The user can see how time is actually distributed across days.

### 6.2 For Planning and Tracking
The user can track events independently from actual logged activity.

### 6.3 For Extensibility
The framework is built to allow future:

- Analytics.
- Planned vs actual comparisons.
- Additional event sources.
- Advanced scheduling features.

Without reworking the canonical model.

---

## 7. Success Criteria (V1)

V1 is successful if:

1. The user can:
   - Initialize a new dataset.
   - Reopen it reliably.
2. Daily Cycles view correctly renders continuous segments.
3. Events Timeline supports:
   - Multi-source overlay.
   - Recurring event rendering.
4. Offline edits persist across restarts.
5. Sync reliably applies queued changes.
6. Conflicts are detected and never silently overwrite data.
7. No silent data loss occurs under expected usage.

---

## 8. User Stories (Representative)

### Activity Domain
- As a user, I want to log what I did between two timestamps.
- As a user, I want to see my entire day rendered as colored segments.
- As a user, I want to edit past days in a focused workspace.

### Event Domain
- As a user, I want to track recurring monthly payments.
- As a user, I want to see all my events across multiple sources in one timeline.
- As a user, I want to zoom in/out to inspect specific periods.

### Infrastructure
- As a user, I want to work offline without losing changes.
- As a user, I want to resolve sync conflicts explicitly.
- As a user, I want to manage multiple event sources independently.

---

## 9. Risks and Constraints

### Technical Risks
- Google Sheets API performance at higher row counts.
- Recurrence rule parsing edge cases.
- Sync complexity with multiple sources.

### UX Risks
- Overlap handling in activity segments (policy not yet locked).
- Complexity creep from too many configuration options.

---

## 10. Future Evolution (Not V1)

Potential V2+ directions:

- Analytics layer.
- Planned vs actual comparison.
- Event-to-activity linking automation.
- Google Calendar live source.
- Timezone migration tools.
- Performance optimizations for 100k+ rows.

These are explicitly deferred beyond V1.