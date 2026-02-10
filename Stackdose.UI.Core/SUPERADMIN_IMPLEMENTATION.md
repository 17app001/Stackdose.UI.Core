# SuperAdmin Implementation Notes

## Purpose

This document summarizes the current SuperAdmin-related design in `Stackdose.UI.Core`.

## Access Levels

Defined in `Models/AccessLevel.cs`:

- `Guest` (0)
- `Operator` (1)
- `Instructor` (2)
- `Supervisor` (3)
- `Admin` (4)
- `SuperAdmin` (5)

## Default Accounts

Default accounts are provisioned by `Services/UserManagementService.cs` when required.

Typical defaults:

- `superadmin` (SuperAdmin)
- `admin01` (Admin)

Refer to the service implementation for authoritative initialization rules.

## Permission Boundaries

- Account-management and high-risk operations are restricted by `SecurityContext` checks.
- Role-based visibility and action constraints are enforced in UI controls and service layer.
- Audit records are written via `ComplianceContext` / `SqliteLogger`.

## Verification Checklist

1. Confirm default-account bootstrap behavior in a clean DB.
2. Verify SuperAdmin can manage all role levels.
3. Verify Admin cannot create/edit SuperAdmin where restricted.
4. Verify denied operations produce audit/system logs.

## Maintenance Rule

When updating role/permission behavior, update both:

- source code rules (`SecurityContext`, `UserManagementService`, related controls)
- this document (high-level behavior only)
