# 10 — Change Log (Spec Architecture Deltas)

Purpose: Track conceptual and structural changes decided after files were drafted, so another LLM can apply updates consistently across the entire spec package.

## CHG-01 — Rename sleep_sheet to activity_sheet (generalize sleep as a category)

### Motivation
User clarified that sleep is not a special entity; it is one of many activities a person does daily. The canonical representation is “from time A to time B I was doing X,” which applies equally to sleep, work, gym, TV, etc. This simplifies the model and aligns with the desired Daily Cycles visualization (continuous time segments across 24 hours).

### Decisions
- Use naming option A1:
  - `sleep_sheet` source type is renamed to `activity_sheet`.
  - Canonical tab `sleep_segments` is renamed to `activity_segments`.
- Sleep is represented as a normal category/label in `categories` (shared taxonomy).
- The “Sleep view” is a preset/filter over categories in view configuration (not a separate data model).
- Day boundary rule remains D2: segments cannot cross midnight; client splits at midnight.

### Required cross-file edits
- 03_happy_path_flows.md:
  - Rename Flow F9 from “Log Sleep / Daily Cycle Segment” to “Log Activity Segment”.
  - Replace references to `sleep_segments` with `activity_segments`.
  - Ensure language reflects category-driven presets (“Sleep” as filter), not special-case storage.
- 04_data_model.md:
  - Rename topology section:
    - “Sleep+Activity” -> “Activity Sheet”.
  - Rename canonical tab definition:
    - `sleep_segments` -> `activity_segments`.
  - Update column IDs from S* to A* (optional but recommended for internal consistency).
- 05_api_contract.md:
  - Replace `sleep_sheet` with `activity_sheet`.
  - Replace `sleep_segments` with `activity_segments` in:
    - Source types
    - Required tabs for activity source
    - Pending queue tab_name examples
- 06_ui_states.md (when written/applied):
  - Rename “Sleep/Daily cycles editor” labels to “Activity Segments”.
  - Keep “Daily Cycles” view; provide a “Sleep preset” default filter.

### Acceptance criteria impact
- Daily Cycles view must render continuous segments (not fixed hourly blocks) using start/end timestamps.
- The system must remain extensible for additional activity categories without schema changes.

---

## CHG-02 — Events and activity_segments are strictly separated (no auto-bridging)

### Motivation
User chose that “events” and “activity segments” represent different domains:
- Events: planned/external/milestones/open-ended tracking objects (agenda-like).
- Activity segments: manual log of what actually happened in time, used for daily cycles.

This avoids hidden automation and keeps data intent explicit.

### Decision
- X1: No automatic creation of activity segments from events (even open-ended events).
- Any correlation is optional and manual via `tag` (events) and `linked_tag` (activity_segments).

### Required cross-file edits
- 03_happy_path_flows.md:
  - Ensure flows for open-ended events (F6/F7) do NOT imply auto-generation of segments.
  - Add note: users may manually log corresponding activity segments if desired.
- 04_data_model.md:
  - Keep `events.tag` and `activity_segments.linked_tag` as optional correlation only.
- 06_ui_states.md:
  - Do not add “auto-log to daily cycle” toggles in V1 UI.

---
## CHG-03 — Stack decision for V1: C#/.NET + WPF + SQLite (portable, native)

### Motivation
Primary constraint is a lightweight, portable, native Windows 10 app with reliable offline-first behavior, safe local caching/queueing, and straightforward OAuth + Google Sheets integration. The user will validate/runtest builds but not maintain the codebase deeply; therefore the stack should minimize packaging and runtime fragility.

### Decision
- V1 implementation stack is:
  - UI: WPF (.NET)
  - Local storage: SQLite (cache + pending queues + source registry)
  - Google integration: OAuth + Google Sheets APIs via .NET libraries
- Distribution target:
  - Portable, self-contained build (copy folder, run on Windows 10 without admin install)

### Required spec updates
- 07_non_functional_requirements.md: add explicit stack and packaging constraints aligned to the above.
- (Optional) README: document build/run expectations for the generated project.

---

## Notes
These changes are approved and should be applied globally for naming and conceptual consistency before implementation.