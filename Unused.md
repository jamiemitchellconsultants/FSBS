# Unused Domain Entity Properties

Properties defined on `FSBS.Domain.Entities` classes that have no dot-access reference
in any code path reachable from `FSBS.Api` (i.e. not found in `FSBS.Api`,
`FSBS.Application`, or `FSBS.Infrastructure.Persistence.Repositories` outside of
EF Core configuration files, migrations, and the entity definitions themselves).

---

## AccountStatement

All properties are unmapped from the API. No endpoint reads or writes `AccountStatement`
rows — the entity exists in the domain and DB but has no application layer coverage yet.

| Property | Type | Notes |
|---|---|---|
| `StatementS3Key` | `string` | S3 key for the generated PDF — never served via API |
| `OpeningBalanceGbp` | `decimal` | Statement period opening balance |
| `ClosingBalanceGbp` | `decimal` | Statement period closing balance |
| `GeneratedBy` | `Guid` | User who triggered generation |
| `GeneratedAt` | `DateTimeOffset` | When the statement was generated |
| `PeriodStart` | `DateOnly` | Start of the statement period |
| `PeriodEnd` | `DateOnly` | End of the statement period |

---

## AppUser

| Property | Type | Notes |
|---|---|---|
| `IsActive` | `bool` | Added to domain but no endpoint reads or filters on it |

---

## Booking

| Property | Type | Notes |
|---|---|---|
| `ProvisionalExpiresAt` | `DateTimeOffset?` | Set at creation but never read back in any query or DTO |

---

## BookingApproval

| Property | Type | Notes |
|---|---|---|
| `RejectionReason` | `string?` | Written by `RejectBookingHandler` but never projected into a DTO or returned by any endpoint |

---

## BookingNote

| Property | Type | Notes |
|---|---|---|
| `IsInternal` | `bool` | Stored but no endpoint filters or returns this flag |

---

## BookingSlot

| Property | Type | Notes |
|---|---|---|
| `LessonId` | `Guid?` | FK to `Lesson` — stored but never read in any query or DTO |

---

## Course

| Property | Type | Notes |
|---|---|---|
| `RegulatoryFramework` | `string?` | Stored but not projected in any course DTO |
| `TotalHours` | `decimal` | Stored but not projected in any course DTO |

---

## Enrolment

| Property | Type | Notes |
|---|---|---|
| `EnrolledAt` | `DateTimeOffset` | Stored but not returned by any enrolment query |

---

## Instructor

| Property | Type | Notes |
|---|---|---|
| `HireDate` | `DateOnly` | Stored but not projected in any instructor DTO |
| `MaxHoursPerWeek` | `short` | Stored but not used in any scheduling constraint check in the API path |

---

## InstructorWeeklyPatternSlot

| Property | Type | Notes |
|---|---|---|
| `PatternId` | `Guid` | FK — set on creation but never read back via dot-access outside the entity |

---

## Invitation

| Property | Type | Notes |
|---|---|---|
| `IssuedAt` | `DateTimeOffset` | Set on creation but never projected into a response DTO |
| `RevokedBy` | `Guid?` | No revoke endpoint exists yet — field is never written or read |
| `RevokedAt` | `DateTimeOffset?` | Same — no revoke flow implemented |

---

## Invoice

| Property | Type | Notes |
|---|---|---|
| `DiscountGbp` | `decimal` | Stored but not projected in any invoice DTO |
| `DueDate` | `DateOnly?` | Stored but not projected in any invoice DTO |
| `PaidAt` | `DateTimeOffset?` | Stored but not projected in any invoice DTO |

---

## Lesson

| Property | Type | Notes |
|---|---|---|
| `RequiresInstructor` | `bool` | Stored but not used in any scheduling or booking validation path |
| `IsMandatory` | `bool` | Stored but not used in any progress or completion check |
| `MinDurationMins` | `int` | Stored but not enforced or projected in any booking flow |

---

## Module

| Property | Type | Notes |
|---|---|---|
| `Description` | `string?` | Added to domain but no endpoint projects it into a DTO |

---

## Qualification

| Property | Type | Notes |
|---|---|---|
| `DocumentS3Key` | `string?` | S3 key stored but no endpoint generates a pre-signed URL for it |
| `ExpiryDate` | `DateOnly?` | Stored but no expiry-check or notification endpoint reads it |
| `IssuedDate` | `DateOnly` | Stored but not projected in any qualification DTO |

---

## ReconfigurationTemplate

| Property | Type | Notes |
|---|---|---|
| `Notes` | `string?` | Added to domain but no endpoint projects it |

---

## Report

| Property | Type | Notes |
|---|---|---|
| `ScheduleCron` | `string?` | Stored but no scheduled execution endpoint reads it |
| `LastRunAt` | `DateTimeOffset?` | Stored but not projected in any report list DTO |
| `IsShared` | `bool` | Stored but no endpoint filters by or returns this flag |
| `DefinitionJson` | `string` | Stored but no endpoint reads the definition back (write-only from API perspective) |

---

## ReportRun

| Property | Type | Notes |
|---|---|---|
| `StartedAt` | `DateTimeOffset?` | Stored but not projected in any report run DTO |
| `ResultS3Key` | `string?` | S3 key stored but no endpoint generates a pre-signed URL for it |
| `ErrorMessage` | `string?` | Stored but not projected in any report run DTO |

---

## ScheduleTemplate

All four time/date range properties are stored but no endpoint reads them back:

| Property | Type | Notes |
|---|---|---|
| `ValidTo` | `DateOnly?` | Stored but not projected in any schedule template DTO |
| `OpenTime` | `TimeOnly` | Stored but not projected in any schedule template DTO |
| `CloseTime` | `TimeOnly` | Stored but not projected in any schedule template DTO |
| `ValidFrom` | `DateOnly` | Stored but not projected in any schedule template DTO |

---

## Summary by category

| Category | Count | Action |
|---|---|---|
| **No endpoint exists yet** | `AccountStatement` (all), `Invitation.RevokedBy/RevokedAt`, `Report.ScheduleCron` | Implement the missing endpoint or feature |
| **Stored but not returned in DTOs** | `Booking.ProvisionalExpiresAt`, `BookingApproval.RejectionReason`, `Course.RegulatoryFramework/TotalHours`, `Enrolment.EnrolledAt`, `Invoice.DiscountGbp/DueDate/PaidAt`, `Lesson.RequiresInstructor/IsMandatory/MinDurationMins`, `Module.Description`, `ReconfigurationTemplate.Notes`, `Report.LastRunAt/IsShared/DefinitionJson`, `ReportRun.StartedAt/ResultS3Key/ErrorMessage`, `ScheduleTemplate.*`, `Qualification.IssuedDate/ExpiryDate` | Add to relevant DTOs and query projections |
| **S3 keys with no pre-signed URL endpoint** | `AccountStatement.StatementS3Key`, `Qualification.DocumentS3Key`, `ReportRun.ResultS3Key` | Add pre-signed URL generation endpoints |
| **Flags stored but never filtered/returned** | `AppUser.IsActive`, `BookingNote.IsInternal`, `BookingSlot.LessonId`, `Instructor.HireDate/MaxHoursPerWeek`, `InstructorWeeklyPatternSlot.PatternId` | Add to DTOs or use in business logic |
