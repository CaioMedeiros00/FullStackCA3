# DriveNow – Viva study notes

Use these notes to understand and explain each part of the project. You must be able to talk through the code yourself.

## 1. Architecture (request flow)
`Page component` → `Service` (CarService, BookingService, ...) → `HttpClient` → REST API.
- Services are registered as **typed HttpClients** in `Program.cs` (`AddHttpClient<CarService>(...)`), so the base URL, timeout and headers are set in one place.
- `ApiServiceBase` holds the shared code: it sends the request, turns non-2xx responses into friendly messages (it reads ProblemDetails or `{ message }` bodies), and catches `HttpRequestException`, timeouts and `JsonException`.
- Every service method returns a `ServiceResult<T>` (`Success`, `Data`, `ErrorMessage`, `StatusCode`, `IsConflict`), so components never need try/catch.

## 2. Render mode
- The whole app uses **Interactive Server** (set in `App.razor`) with `prerender: false`.
- Why: the logged-in user is read from `ProtectedSessionStorage`, which needs the browser connection, and that doesn't exist during prerendering.

## 3. Authentication & authorization
- `DriveNowAuthStateProvider : AuthenticationStateProvider`
  - `GetAuthenticationStateAsync()` restores the user from encrypted session storage the first time it runs, then uses the copy in memory. It also checks the 8-hour timeout.
  - `SignInAsync` / `SignOutAsync` save or clear the session and call `NotifyAuthenticationStateChanged` so the UI updates straight away.
- `AuthService` checks the username and password against hardcoded accounts. Passwords are stored as SHA-256 hashes and compared with `CryptographicOperations.FixedTimeEquals`.
- **Protected pages**: `MainLayout` wraps everything in `<AuthorizeView>`. If the user isn't logged in, `<RedirectToLogin />` sends them to `/login?returnUrl=...`. The Login page uses `EmptyLayout`, so it isn't protected.
- `SafeReturnUrl()` only allows relative URLs, which prevents open redirects.
- **Roles**: Admin and Staff. `<AuthorizeView Roles="Admin">` hides the delete buttons and the Reports link. `DeleteCarAsync` also checks `user.IsInRole("Admin")`, because hiding a button alone is not security.

## 4. Components, Parameters & EventCallback
- `SearchBox`: `Value` + `ValueChanged` (an `EventCallback<string>`), which enables `@bind-Value`.
- `ConfirmDeleteModal`: `IsVisible`, `ItemName`, `IsProcessing`, plus `OnConfirm` / `OnCancel` callbacks. It is built on top of `AppModal`, which reuses one component inside another.
- `CarForm` / `BookingForm`: the parent passes the model in and gets it back through `OnValidSubmit` (`EventCallback<Car>`). The parent page decides how to save it.
- `DashboardCard`, `StatusBadge`, `PageHeader`, `EmptyState`, `AppAlert` (with `ChildContent` for a Retry button), `Pagination` (`EventCallback<int>`).

## 5. Validation
- Client side: `EditForm` + `DataAnnotationsValidator` + `ValidationMessage` with the annotations in `Models/Car.cs` and `Models/Booking.cs`.
- Custom attributes: `[NotInPast]` and `[DateGreaterThan(nameof(StartDate))]` (in `Validation/`).
- Rules that annotations can't express:
  - unique registration: `CarService.IsRegistrationTaken`
  - overlapping bookings: `BookingRules.Overlaps` (a.Start < b.End && b.Start < a.End). The form warns live, `BookingService.CreateBookingAsync` re-checks with fresh data, and a 409 from the API is also handled.
- Server side (local API): `Validator.TryValidateObject` returns 400 with errors, and conflicts return 409.

## 6. Car availability logic
- *Active booking* = `StartDate <= now < EndDate` (`BookingRules.IsActive`).
- When a booking is created and is already active, `BookingService` calls `CarService.SetAvailabilityAsync(car, false)` (a PUT).
- When a booking is deleted and the car has no other active booking, the car is set back to available.
- `SyncCarAvailabilityAsync` (run when the Dashboard and Bookings pages load) marks a car unavailable once a future booking starts.
- The UI shows **Booked** when a car has an active booking, **Unavailable** when staff took it out of service, and **Available** otherwise.
- A car with an active or upcoming booking cannot be deleted. The page checks first, and a 409 from the API is also handled.

## 7. State management
- `AppState` (scoped, so one per user circuit) has an `OnChange` event.
  - Pages call `AppState.NotifyDataChanged()` after creating, updating or deleting.
  - `Home.razor` subscribes and reloads. `NavMenu` subscribes and updates the active-bookings badge.
  - It also remembers the Cars and Bookings filters when you move between pages.
- `ToastService` (toast notifications) and `ThemeService` (dark mode via JS interop and localStorage) follow the same event pattern.
- Components unsubscribe in `Dispose()` to avoid memory leaks.

## 8. Error handling
- Every page has three states: **loading** (`LoadingSpinner`), **error** (`EmptyState` with a Try again button), and **empty** or **no results**.
- The Dashboard loads all three APIs in parallel with `Task.WhenAll`. If one fails it shows "—" for that card plus a warning, and the rest still works.

## 9. Things to demo in the viva
1. Open `/cars` while logged out: you are redirected to login, then sent back to Cars after logging in.
2. Submit an empty login form to show the validation messages, then try a wrong password.
3. Dashboard numbers, then Refresh.
4. Cars: search, the Available-only filter, adding a car with invalid data, a duplicate registration, editing, deleting a booked car (blocked) and deleting a free car.
5. Bookings: an overlapping booking (live warning), a valid booking (the car becomes Booked and the nav badge updates), then cancel it.
6. Locations: search, then the details page.
7. Log in as `staff`: no delete buttons, no Reports page.
8. Dark mode, CSV export, and a mobile-width browser window.
