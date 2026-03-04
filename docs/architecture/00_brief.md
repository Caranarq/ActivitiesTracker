# Time-Tracked Life Framework (Windows) — Brief

## Version Scope
This specification defines V1 (first usable production version).

## One-liner
A portable Windows 10 desktop application for tracking time-based activities (including sleep as a category) and externally scheduled events, using Google Sheets as the canonical source of truth and supporting configurable visualizations beyond traditional Gantt views.

## Target User
- Single primary user (daily use).
- Future possibility: optional read/share access to other users’ datasets.

## Core Principle
The system tracks “things that happen in time.”
All time-based records are stored in Google Sheets (canonical source of truth).
The Windows application is a portable client that reads/writes Sheets via OAuth and can work offline with later sync.

Separate Sheets:
- Sheet A: Activity Sheet data (includes sleep as any other category).
- Sheet B: Events.

## Platform Constraints
- Windows 10.
- Portable application (no admin installation required).
- OAuth login for Google access.
- Data must never leave user control except Google Sheets.

## Core Record Types

### 1. Activity/Event

Temporal types:
- Duration (start + end timestamp).
- Instant (single timestamp).
- Open-ended (start timestamp; end null until closed).

Attributes:
- Unique ID
- Category / Type
- Start timestamp
- End timestamp (nullable)
- Timezone at capture (explicitly stored)
- Optional tag
- Extensible metadata field

Relationships and CPM logic are explicitly out of scope for V1.

### 2. Activity Segment (Daily Cycles)

- Precise timestamps (no fixed 15-minute blocks).
- Manual entry required in V1.
- Paste supported.
- “Insert current timestamp” shortcut required.
- Timezone stored per record.

Daily view renders one row per date with color-coded segments based on configurable labels.

### 3. Recurring Event (Rule-Based)

Recurring events are stored as recurrence rules (not pre-generated instances).

Must support:
- Every X week of X month
- Last day of month
- Standard interval-based recurrence

The application generates occurrences dynamically for visualization.

## Views (V1)

1. Events Overview Timeline
- Time-axis view (Gantt-like).
- Supports duration, instant, and open-ended records.
- Configurable filters, grouping, and color mapping.

2. Daily Cycles (with presets such as Sleep-only)
- Date rows on left.
- Horizontal segmented bar per date.
- Color-coded by category.
- Optimized for visual pattern recognition.

## Configuration (V1)

- Create/edit/delete categories.
- Configure color mappings.
- Configure view filters and grouping.
- Persist configuration data in Sheets (or associated metadata sheet).

## Offline and Sync Model

- Offline-first client.
- Local change queue.
- Sync to Sheets when connection restored.
- Google Sheets remains canonical.

Conflict resolution strategy: TBD (to be defined in NFR file).

## Data Sources

V1:
- Google Sheets only (canonical).

Future:
- Additional live data sources (e.g., Google Calendar) supported by extensible architecture.

## Non-Goals (V1)

- CPM scheduling engine.
- Relationship logic (FS/SS/FF/SF).
- Critical path calculation.
- Automatic internet ingestion of external schedules.
- Wearable integrations.
- Third-party plugin runtime.

## Success Criteria

- User can initialize required schema in two Sheets.
- User can reopen dataset reliably.
- User can log/edit duration, instant, and open-ended activities.
- Recurring events render correctly.
- Activity patterns (including sleep presets) render correctly.
- View configuration persists.
- Offline mode works and syncs safely.
