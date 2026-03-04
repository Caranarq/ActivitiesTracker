# 02 — Users and Permissions (V1)

This document defines user roles, authentication model, and permission boundaries for V1.

---

## 1. User Model (V1)

### 1.1 Single-User System

V1 is strictly designed for a single primary user.

There is:
- No internal multi-user account system.
- No internal role-based access control.
- No shared in-app user management.

The authenticated Google account is treated as the sole identity authority for accessing Google Sheets.

---

## 2. Authentication Model

### 2.1 Identity Provider

Authentication is performed via:

- Google OAuth (interactive login).

The Google account determines:
- Access rights to specific spreadsheets.
- Permission to read/write those spreadsheets.

The application does NOT:
- Store passwords.
- Implement its own authentication system.
- Issue user tokens outside OAuth.

---

## 3. Authorization Model

### 3.1 Spreadsheet-Level Permissions

Authorization is entirely delegated to Google:

- If the authenticated Google account has access to a spreadsheet, the app can read/write it.
- If not, the app must block access and show a clear error.

The app does not implement row-level or category-level permissions.

---

## 4. Source-Level Access

### 4.1 Activity Sheet

Exactly one `activity_sheet` source is active at a time.

The authenticated user must:
- Have read/write access to the spreadsheet.
- Be able to create tabs during schema initialization (if using "Create Dataset").

### 4.2 Event Sources

Multiple `google_sheets_events` sources may be active.

Each source:
- Uses the same OAuth session.
- Requires independent spreadsheet access rights.

Conflicts and sync behavior are handled per source.

---

## 5. Local Machine Access Assumption

V1 assumes:

- The local Windows user account is trusted.
- No in-app password protection is required.

The application does NOT:
- Provide user switching.
- Provide local data encryption by default (subject to future decision in NFR).
- Restrict access to local cache by multiple Windows users (relies on OS-level user directory isolation).

---

## 6. Future Multi-User Considerations (Not V1)

Future versions may consider:

- Shared dataset viewing.
- Read-only modes.
- Per-source permission policies.
- Multi-account login switching.

These are explicitly out of scope for V1.

---

## 7. Security Boundary Definition

In V1:

- Data leaves the local machine only when interacting with:
  - Google OAuth endpoints.
  - Google Sheets API endpoints.
- No telemetry, analytics, or third-party integrations exist.

The system boundary is:
Local machine <-> Google services only.

---

## 8. Responsibility Model

The user is responsible for:

- Managing Google account security.
- Managing spreadsheet sharing permissions.
- Ensuring backups of Google Sheets if desired.

The application is responsible for:

- Not exposing credentials.
- Not leaking data to unintended destinations.
- Preserving local edits reliably.