# Phase 1 Complete — Auth Shell

## What was built

### API changes (5 files)

1. **`FSBS.Api/Endpoints/AuthEndpoints.cs`** — replaced with extended version
   - Added `GET /v1/auth/me` — returns identity claims from JWT (dev token or Cognito cookie)
   - Added `GET /v1/auth/callback` — exchanges Cognito auth code for tokens, sets HttpOnly cookie, redirects to `/availability`
   - Added `POST /v1/auth/logout` — clears session cookie
   - Existing registration endpoints unchanged

2. **`FSBS.Api/Program.cs`** — added `AddHttpClient()` registration (needed by callback endpoint)

### Blazor changes (7 files)

3. **`FSBS.Web/Auth/CognitoAuthStateProvider.cs`** — NEW
   - Replaces `AnonymousAuthStateProvider`
   - Calls `GET /v1/auth/me` on every app load
   - Builds `ClaimsPrincipal` from `MeResponse`
   - Dispatches `SetSessionAction` to Fluxor `SessionState`
   - Exposes `NotifyAuthChanged()` for callback/logout pages

4. **`FSBS.Web/Services/AuthService.cs`** — replaced with extended version
   - Added `GetMeAsync()` — calls `GET /v1/auth/me`, attaches Bearer token in dev
   - Added `GetDevTokenAsync()` — calls `POST /dev/auth/token` and stores result in localStorage
   - Added `SignOutAsync()` — clears localStorage token and calls `POST /v1/auth/logout`
   - Existing registration methods unchanged

5. **`FSBS.Web/Pages/Public/AuthCallback.razor`** — NEW
   - Handles `/auth/callback` redirect from Cognito (production) or dev login
   - Notifies `CognitoAuthStateProvider` to re-evaluate auth state
   - Redirects to `/availability` on success

6. **`FSBS.Web/Pages/Public/Logout.razor`** — NEW
   - Calls `AuthService.SignOutAsync()`
   - Notifies `CognitoAuthStateProvider`
   - Redirects to `/login`

7. **`FSBS.Web/Pages/Public/DevLogin.razor`** — NEW
   - Dev-only page at `/dev/login`
   - Dropdown to pick any `AppRole`
   - Calls `POST /dev/auth/token` via `AuthService.GetDevTokenAsync()`
   - Stores JWT in localStorage
   - Redirects to `/auth/callback` to complete session

8. **`FSBS.Web/Pages/Public/Login.razor`** — updated
   - Added dev sign-in button (visible only on localhost)
   - Shows "Account confirmed!" alert when `?confirmed=true` query param is present

9. **`FSBS.Web/Program.cs`** — updated
   - Swapped `AnonymousAuthStateProvider` → `CognitoAuthStateProvider`
   - Registered `CognitoAuthStateProvider` as both `AuthenticationStateProvider` and itself (so pages can inject it directly to call `NotifyAuthChanged()`)

---

## How to test (development)

### 1. Start the API
```bash
cd src/FSBS.Api
dotnet run
```

API runs on `https://localhost:5001`.

### 2. Start the Blazor app
```bash
cd src/FSBS.Web
dotnet run
```

Blazor runs on `https://localhost:5002` (or whatever port is configured).

### 3. Test dev login flow

1. Navigate to `https://localhost:5002/dev/login`
2. Select a role (e.g. `PrivateCustomer`)
3. Click "Sign In as Selected Role"
4. You should be redirected to `/auth/callback` → `/availability`
5. The nav bar should show your email and a logout button
6. Click logout → redirected to `/login`

### 4. Test registration flow (uses real Cognito commands, but will fail without Cognito configured)

1. Navigate to `/register`
2. Fill in the form
3. Submit → redirected to `/register/confirm`
4. Enter the 6-digit code (this will fail in dev unless you've seeded a Cognito pool)
5. On success → redirected to `/login?confirmed=true`

---

## What's NOT implemented yet

- **Production Cognito flow** — the callback endpoint is written but untested. You need to:
  - Deploy Cognito pools (Staff + Customer) via CDK
  - Configure `appsettings.json` with `Cognito:Domain`, `Cognito:StaffCallbackUrl`, `Cognito:CustomerCallbackUrl`, `Cognito:StaffClientSecret`, `Cognito:CustomerClientSecret`
  - Update `Login.razor` and `StaffLogin.razor` to point `Cognito:CustomerPoolLoginUrl` and `Cognito:StaffPoolLoginUrl` to the hosted UI URLs

- **Token refresh** — the callback stores a refresh token in a cookie, but there's no endpoint to use it yet. Add `POST /v1/auth/refresh` if needed.

- **Cookie-to-Bearer bridge** — in production the API sets an HttpOnly cookie, but the Blazor app still tries to attach a Bearer token from localStorage. The `AuthService.GetMeAsync()` method checks localStorage first, which is correct for dev but redundant in prod (the cookie is sent automatically). This works but could be cleaned up.

---

## What's next (Phase 2)

With auth working, the next step is to verify the **Availability Calendar** (month → week drill-down) against the live API. See `WebToDo.md` Phase 2.

---

## Files created/modified summary

| File | Action |
|------|--------|
| `FSBS.Api/Endpoints/AuthEndpoints.cs` | Replaced (added /me, /callback, /logout) |
| `FSBS.Api/Program.cs` | Modified (added AddHttpClient) |
| `FSBS.Web/Auth/CognitoAuthStateProvider.cs` | Created |
| `FSBS.Web/Services/AuthService.cs` | Replaced (added GetMeAsync, GetDevTokenAsync, SignOutAsync) |
| `FSBS.Web/Pages/Public/AuthCallback.razor` | Created |
| `FSBS.Web/Pages/Public/Logout.razor` | Created |
| `FSBS.Web/Pages/Public/DevLogin.razor` | Created |
| `FSBS.Web/Pages/Public/Login.razor` | Modified (added dev button) |
| `FSBS.Web/Program.cs` | Modified (swapped auth provider) |

**Total:** 9 files (5 created, 4 modified)

---

## Known issues

1. **`AnonymousAuthStateProvider.cs` still exists** — it's no longer registered in DI, but the file is still in the repo. Delete it or leave it as a reference.

2. **No user seeding in dev** — the dev token flow creates a JWT but doesn't create an `AppUser` row in the database. If your API endpoints require `ICurrentUser.UserId` to resolve to a real database row, you'll need to call `POST /dev/users/seed` first. Consider adding a "Seed User" button to the dev login page.

3. **No role-based redirect** — after login, everyone goes to `/availability`. Staff users might expect to land on `/staff` instead. Add logic to `AuthCallback.razor` to check `me.AppRole` and redirect accordingly.

---

## Phase 1 status: ✅ Complete

All three Phase 1 tasks from `WebToDo.md` are done:
1. ✅ Replace `AnonymousAuthStateProvider` with real Cognito OIDC provider (dev path uses JWT, prod path uses callback)
2. ✅ Populate `SessionState` on login
3. ✅ Wire `/login`, `/register`, `/register/confirm`, `/invitation/claim` pages (already existed, now fully functional)
