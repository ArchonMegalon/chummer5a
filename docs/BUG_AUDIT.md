# Bug Audit Report

Date: 2026-03-01

## Scope

Quick static audit of core app (`Chummer`), web API (`ChummerHub`), and crash utility (`CrashHandler`) for correctness and security defects.

## Findings

### 1) Insecure email transport configuration allows MITM and deprecated TLS

**Severity:** Critical  
**Where:** `ChummerHub/Services/EmailSender.cs`

`HttpClientHandler` is configured to trust **all certificates** and explicitly enables obsolete protocols (`Ssl3`, `Tls`, `Tls11`) alongside `Tls12`.

- `ServerCertificateCustomValidationCallback = ... => true` disables certificate validation entirely.
- `SslProtocols` includes SSLv3/TLS 1.0/1.1, which are deprecated and vulnerable.

**Why this is a bug:** Outbound email API calls can be intercepted or downgraded in transit. This is a direct security vulnerability, not just a code-quality issue.

**Recommended fix:**
- Remove the custom validation callback override, or gate it behind explicit local-development-only checks.
- Restrict protocols to modern TLS only (prefer OS defaults or TLS 1.2+ / 1.3 depending on target framework/runtime).

---

### 2) HTML is constructed from unencoded user/group fields (injection risk)

**Severity:** High  
**Where:** `ChummerHub/Controllers/V1/ChummerController.cs`

The `G` action builds raw HTML via `StringBuilder.AppendFormat` and inserts values like `GroupName`, `Description`, and serialized JSON directly into attribute values without HTML encoding.

**Why this is a bug:** If any value contains quotes or HTML-significant characters, generated markup can break or become injectable (reflected/stored HTML injection depending on source data). Hidden inputs are not immune to attribute injection.

**Recommended fix:**
- Use framework-safe HTML generation or encode all values with `HtmlEncoder.Default.Encode(...)` before interpolation.
- Prefer returning a strongly typed view/model or JSON payload rather than manual HTML concatenation.

---

### 3) Sync-over-async calls in request path (`.Result`) can cause thread starvation/deadlocks

**Severity:** Medium  
**Where:** `ChummerHub/Controllers/V1/ChummerController.cs`

Inside a request handler, code blocks on async operations via `.Result`:
- `_signInManager.UserManager.GetUserAsync(User).Result`
- `sg.GetGroupMembers(_context, false).Result`

**Why this is a bug:** Blocking async calls in ASP.NET request paths can degrade throughput and has deadlock risk depending on synchronization/context behavior.

**Recommended fix:**
- Convert action to `async Task<IActionResult>` and `await` these calls.

---

### 4) Unsafe fallback assumes non-empty settings collection

**Severity:** Medium  
**Where:** `Chummer/Backend/Characters/Character.cs`

If both default setting lookups fail, constructor falls back to:

`SettingsManager.LoadedCharacterSettings.First().Value`

**Why this is a bug:** If settings are missing/uninitialized (corrupt install, startup race, first-run failure), `First()` throws `InvalidOperationException` and prevents character construction.

**Recommended fix:**
- Guard with `Any()` or `FirstOrDefault()` + null handling.
- Provide a resilient fallback (`new CharacterSettings()`) and log the configuration error.

## Notes

This pass focused on high-confidence issues identifiable via source inspection. A deeper audit should add:
- automated SAST,
- route-level fuzzing for `ChummerHub`,
- and startup fault-injection tests around settings initialization.
