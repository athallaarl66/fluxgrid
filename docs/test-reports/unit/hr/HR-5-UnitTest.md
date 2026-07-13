# HR-5 Automatic CV Parsing (AI Groq) — Unit Test Report

**Generated:** 2026-07-13
**Project:** FluxGrid ERP
**Test Framework:** xUnit + Moq + EF Core InMemory
**Test Runner:** dotnet test

---

## 1. Test Execution Summary

| Metric | Value |
|---|---|
| Total Tests | 22 |
| Passed | 22 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~2 s |
| Test File | `FluxGrid.Api.Tests.dll` |
| Test Project | `tests/unit/hr/hr-5-automatic-cv-parsing.Test/` |

---

## 2. Test Results Overview

### 2.1 TextExtractorTests — 3 tests

| Test | Status | Duration |
|---|---|---|
| `IsScannedDocument_ReturnsTrue_WhenTextBelow50Chars` | ✅ Passed | < 1 ms |
| `IsScannedDocument_ReturnsFalse_WhenTextIs50OrMoreChars` | ✅ Passed | < 1 ms |
| `IsScannedDocument_ReturnsTrue_WhenTextIsEmpty` | ✅ Passed | < 1 ms |

### 2.2 GroqApiServiceTests — 11 tests

| Test | Status | Duration |
|---|---|---|
| `RedactPii_ReplacesEmail` | ✅ Passed | < 1 ms |
| `RedactPii_ReplacesPhone` | ✅ Passed | < 1 ms |
| `RedactPii_ReplacesInternationalPhone` | ✅ Passed | < 1 ms |
| `RedactPii_ReplacesAddress` | ✅ Passed | < 1 ms |
| `RedactPii_HandlesMultiplePii` | ✅ Passed | < 1 ms |
| `RedactPii_DoesNotModifyCleanText` | ✅ Passed | < 1 ms |
| `TruncateToTokens_DoesNotTruncate_WhenWithinLimit` | ✅ Passed | < 1 ms |
| `TruncateToTokens_Truncates_WhenExceedsLimit` | ✅ Passed | < 1 ms |
| `TruncateToTokens_ReturnsEmpty_WhenInputIsEmpty` | ✅ Passed | < 1 ms |
| `TruncateToTokens_ReturnsNull_WhenInputIsNull` | ✅ Passed | < 1 ms |

### 2.3 RecruitmentServiceApprovalTests — 8 tests

| Test | Status | Duration |
|---|---|---|
| `ApproveCandidateAsync_SetsStatusToActive` | ✅ Passed | < 1 ms |
| `ApproveCandidateAsync_Throws_WhenCandidateNotFound` | ✅ Passed | < 1 ms |
| `ApproveCandidateAsync_Throws_WhenStatusIsNotParsed` | ✅ Passed | < 1 ms |
| `ApproveCandidateAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `RejectCandidateAsync_SetsStatusToRejected` | ✅ Passed | < 1 ms |
| `RejectCandidateAsync_Throws_WhenCandidateNotFound` | ✅ Passed | < 1 ms |
| `RejectCandidateAsync_Throws_WhenStatusIsNotParsed` | ✅ Passed | < 1 ms |
| `DeleteCandidateAsync_RemovesCandidateAndFile` | ✅ Passed | < 1 ms |
| `DeleteCandidateAsync_Throws_WhenCandidateNotFound` | ✅ Passed | < 1 ms |

---

## 3. Coverage by Feature

| Feature Area | Tests | Scope |
|---|---|---|
| **Scanned Document Detection** | 3 | Empty text (<50 chars), boundary (50 chars), scanned PDF fallback |
| **PII Redaction** | 6 | Email, phone (local + international), address (Indonesian Jl./Gang/etc.), combined PII, clean text passthrough |
| **Token Truncation** | 4 | Within limit, exceeds limit, empty input, null input |
| **Candidate Approval** | 4 | Status → ACTIVE, not-found guard (404), wrong-status guard (DRAFT/ACTIVE rejected), tenant isolation |
| **Candidate Rejection** | 3 | Status → REJECTED, not-found guard, wrong-status guard |
| **Candidate Deletion** | 2 | Removes DB record + calls storage delete, not-found guard |

---

## 4. Test Configuration

- **Database:** EF Core InMemory (isolated per test class via `Guid.NewGuid()` database name)
- **Mocking:** `IFileStorageService` mocked via Moq for delete verification; `IConfiguration` mocked for bucket name; `IServiceScopeFactory` mocked for scoped parsing trigger
- **External deps:** None — Groq API not required for unit tests; GroqApiService static methods tested directly
- **Backend entry point:** `FluxGrid.Api` project referenced directly

---

## 5. Verification Checklist

| Requirement | Covered By |
|---|---|
| Scanned PDF (< 50 chars) skips Groq | `IsScannedDocument_ReturnsTrue_WhenTextBelow50Chars` |
| Normal PDF text proceeds to Groq | `IsScannedDocument_ReturnsFalse_WhenTextIs50OrMoreChars` |
| PII removed before Groq call | `RedactPii_ReplacesEmail`, `Phone`, `Address`, `InternationalPhone`, `MultiplePii` |
| Clean text unchanged | `RedactPii_DoesNotModifyCleanText` |
| Text truncated to ~4000 tokens | `TruncateToTokens_Truncates_WhenExceedsLimit` |
| Empty/null text handled | `TruncateToTokens_ReturnsEmpty`, `ReturnsNull` |
| Approve PARSED → ACTIVE | `ApproveCandidateAsync_SetsStatusToActive` |
| Reject PARSED → REJECTED | `RejectCandidateAsync_SetsStatusToRejected` |
| Approve/reject not-found = 404 | `*CandidateNotFound` tests |
| Approve/reject wrong status = 400 | `*StatusIsNotParsed` tests |
| Tenant isolation enforced | `ApproveCandidateAsync_RespectsTenantIsolation` |
| Delete removes record + storage file | `DeleteCandidateAsync_RemovesCandidateAndFile` |
| Delete not-found = 404 | `DeleteCandidateAsync_Throws_WhenCandidateNotFound` |

---

## 6. Ship Readiness

**✅ Ready** — all 22 tests pass. Core parsing pipeline (PII redaction, token truncation, scanned doc detection) and approval workflow (approve, reject, delete with proper guards and tenant isolation) are covered. No regressions detected.

> **Note:** End-to-end parsing flow (file read → text extract → Groq API → persist) requires integration tests with real file I/O and a live Groq API key. Those are covered under HR-5 E2E test plan.
