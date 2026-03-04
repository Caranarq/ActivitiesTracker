# Implementation & Delivery Requirements — V1

This document defines how the implementation must be delivered so that a non-developer user can run and test the application.

---

## 1. Technology Stack (Fixed)

The implementation MUST use:

- Language: C#
- Framework: .NET (WPF)
- Local storage: SQLite
- Google integration: OAuth + Google Sheets API (official .NET libraries)
- Target OS: Windows 10

No alternative stack is allowed for V1.

---

## 2. Build Output Requirement (Critical)

The final deliverable MUST include:

Option A (Preferred):
- A portable, self-contained build folder:
  - /dist/ (or /publish/)
  - Application.exe
  - All required DLLs
  - No installation required
  - Must run via double-click on Windows 10

Option B (If A is not possible):
- Full source code
- A single documented command to build a self-contained portable version, e.g.:

  dotnet publish -c Release -r win-x64 --self-contained true

- Clear indication of where the output folder is located.

---

## 3. No Developer Tools Required for Running

The user running the app:

- Must NOT need Visual Studio to run it.
- Must NOT need admin privileges.
- Must NOT manually edit project files.

If a runtime is required, the build must be self-contained.

---

## 4. OAuth Setup Documentation (Required)

The implementation MUST include:

A file:
- README_RUN.md

It must clearly explain:

1. How to create a Google OAuth Desktop Application in Google Cloud Console.
2. What scopes are required.
3. Where to place the credentials.json file.
4. How the app loads credentials.
5. How first-time authentication works.

The instructions must be step-by-step.

---

## 5. Project Structure Requirements

The project must:

- Follow a clean folder structure:
  - /src
  - /Infrastructure
  - /Domain
  - /UI
  - /Sync
  - etc. (logical separation)
- Clearly separate:
  - UI logic
  - Data model
  - Sync logic
  - Google API integration

No giant single-file implementation.

---

## 6. Logging and Debug Visibility

The implementation must:

- Log sync operations to a local file.
- Log conflicts.
- Log schema validation errors.
- Log OAuth errors.

Logs must be stored locally only.

---

## 7. Configuration Handling

The implementation must:

- Store local cache in a user-scoped folder.
- Not hardcode paths.
- Clearly define where local database is stored.

---

## 8. Failure Handling

The app must:

- Never crash silently.
- Show user-readable errors.
- Preserve local data even if remote sync fails.

---

## 9. Validation Before Delivery

Before considering the implementation complete, it must:

- Successfully create a new Activity Sheet schema.
- Successfully open existing schemas.
- Create activity segments.
- Create events.
- Create recurring events.
- Work offline.
- Sync correctly.
- Detect and resolve a simulated conflict.

---

## 10. Delivery Format

The final delivery must include:

- Source code.
- Portable build output.
- README_RUN.md
- README_BUILD.md (if build is required).

No partial delivery.