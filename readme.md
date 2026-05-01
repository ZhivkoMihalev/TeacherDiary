# TeacherDiary

TeacherDiary is a classroom activity tracking platform that helps teachers monitor student progress and drive engagement through gamification. The platform covers reading, assignments, and custom challenges while rewarding students with points, streaks, badges, and leaderboard rankings.

---

## Features

### Teacher
- Create and manage classes
- Assign books to classes with start/end dates
- Create homework assignments with due dates
- Create time-bound challenges (read X pages, finish X books, complete X assignments)
- Monitor per-student and class-wide activity
- View an analytics dashboard with top readers, streaks, and recent badges
- Enroll and remove students from classes

### Parent
- Create student profiles for their children
- Track reading progress per book
- Track assignment completion
- Update their child's reading page and mark assignments as done
- View full student detail: activity history, learning activities, stats

### Gamification
- Points awarded for reading (per page) and assignment completion
- Daily streak tracking — maintained as long as the student is active each day
- Five badge types awarded automatically on milestones
- Per-class points leaderboard
- Best-streak leaderboard

---

## Architecture

The solution follows **Clean Architecture** with four projects:

```
TeacherDiary/
├── TeacherDiary.Domain          # Entities, enums, domain models
├── TeacherDiary.Application     # Interfaces, DTOs, Result<T> pattern
├── TeacherDiary.Infrastructure  # EF Core, service implementations, JWT
└── TeacherDiary.Api             # ASP.NET Core controllers, middleware, startup
```

**Dependency rule:** Api → Application ← Infrastructure; Domain has no outward dependencies.

**Key patterns:**
- `Result<T>` — all service methods return `Result<T>` (never throw for expected failures)
- Unit of Work — sub-services (gamification, badges, learning activity updates) do not call `SaveChangesAsync`; the top-level caller persists everything in one transaction
- `ICurrentUser` — resolves `UserId`, `OrganizationId`, and role from the JWT via `IHttpContextAccessor`

---

## Notifications

### Architectural Design — Domain Events

Notifications are driven by the **Domain Events pattern**. Services do not create notifications directly; instead they publish a typed event, and a dedicated handler creates the notification. This keeps business logic decoupled from the notification layer.

```
Application layer defines:
  IDomainEvent
  IDomainEventHandler<TEvent>
  IEventDispatcher

Infrastructure layer implements:
  EventDispatcher          — resolves all registered handlers for an event and calls them
  NotificationHandler×12  — one handler per event type, creates the notification record(s)
  NotificationService      — persists to DB and pushes in real time via SignalR
```

Because `NotificationHub` lives in the API project (to avoid a circular dependency), the push step is hidden behind an `INotificationPusher` abstraction defined in the Application layer and implemented in the API layer as `HubNotificationPusher`.

---

### Notification Flow — User-Triggered

When a user takes an action (e.g. a student completes an assignment), the following happens synchronously inside the same HTTP request:

```
StudentSelfService.CompleteAssignmentAsync()
  └─ eventDispatcher.PublishAsync(new AssignmentCompletedEvent(...))
        └─ AssignmentCompletedNotificationHandler.HandleAsync()
              └─ notificationService.CreateAsync(teacherId, ...)
                    ├─ INSERT INTO Notifications
                    └─ HubNotificationPusher.PushAsync()
                          └─ SignalR → WebSocket → teacher's browser (real time)
```

The HTTP response is returned only after the notification has been persisted and pushed.

---

### Notification Flow — Background Service (Scheduled)

`OverdueAndReminderService` is an ASP.NET Core `BackgroundService` that runs in a continuous loop alongside the API:

```
On startup:
  API (HTTP requests)  ──┐
                          ├── run in parallel inside the same process
  OverdueAndReminderService ──┘

Loop (every 1 hour):
  1. Find assignments whose DueDate fell within the last hour → AssignmentOverdueEvent
  2. Find assigned books whose EndDate fell within the last hour → BookOverdueEvent
  3. Between 19:00–20:00 UTC once per day:
       Find students with an active streak who haven't logged activity today → StreakReminderEvent
  4. Sleep 1 hour, repeat
```

To avoid duplicate notifications, overdue events store the assignment/book ID in `Notification.ReferenceId`. Before dispatching, the service queries existing notifications of that type and excludes already-notified IDs.

Because `BackgroundService` is a singleton but `AppDbContext` is scoped, each loop iteration creates a fresh DI scope via `IServiceScopeFactory`.

---

### Notification Types

| Type | Trigger | Recipients |
|---|---|---|
| `AssignmentCreated` | Teacher creates an assignment | Students and parents in the class |
| `AssignmentCompleted` | Student or parent marks assignment done | Teacher |
| `AssignmentOverdue` | Background service (deadline passed) | Teacher, students, and parents in the class |
| `BookAssigned` | Teacher assigns a book | Students and parents in the class |
| `BookCompleted` | Student finishes a book | Teacher |
| `BookOverdue` | Background service (end date passed) | Teacher, students, and parents in the class |
| `ChallengeCreated` | Teacher creates a challenge | Students and parents in the class |
| `ChallengeCompleted` | Student or parent completes a challenge | Teacher |
| `BadgeEarned` | Badge evaluation engine awards a badge | Student and their parent |
| `StreakReminder` | Background service (19:00–20:00 UTC daily) | Students with an active streak who haven't logged activity today |
| `StreakBroken` | Activity service detects a broken streak | Student and their parent |
| `StudentJoinedClass` | Teacher enrolls a student | Teacher |

---

### Real-Time Delivery

The frontend connects to `/hubs/notifications` over WebSocket using `@microsoft/signalr`. Because WebSockets cannot set custom headers, the JWT token is passed as an `access_token` query parameter and extracted via the `JwtBearerEvents.OnMessageReceived` hook.

Each authenticated user is placed in a SignalR group keyed by their user ID. `HubNotificationPusher` sends to that group, so notifications are delivered only to the intended recipient even if multiple browser tabs are open.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (SQL Server) |
| Authentication | JWT Bearer |
| Logging | Serilog (structured, request logging) |
| API docs | Swagger / OpenAPI (XML comments) |
| Testing | xUnit, Moq, EF Core InMemory / SQLite |

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (local or remote)

### Running the API

```bash
cd TeacherDiary.Api
dotnet run
```

On startup the application:
1. Applies any pending EF Core migrations automatically
2. Seeds the badge catalog if empty
3. Serves Swagger UI at `/swagger`

---

## Authentication

All protected endpoints require a JWT Bearer token:

```
Authorization: Bearer <token>
```

Tokens are obtained from the login or registration endpoints. The token payload includes:
- `sub` — user ID
- `role` — `Teacher` or `Parent`
- `organizationId` — teacher's organization ID (absent for parents)

Token lifetime is controlled by `Jwt:ExpiresInMinutes` (default 24 hours).

---

## API Reference

### Authentication — `api/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register-teacher` | — | Register a new teacher account |
| POST | `/api/auth/register-parent` | — | Register a new parent account |
| POST | `/api/auth/login` | — | Log in, receive JWT token |

---

### Classes — `api/classes`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/classes` | Teacher | List classes owned by the current teacher |
| POST | `/api/classes` | Teacher | Create a new class |
| DELETE | `/api/classes/{classId}` | Teacher | Delete a class |

---

### Students — various

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/classes/{classId}/students` | Teacher | List students enrolled in a class |
| POST | `/api/classes/{classId}/students/{studentId}` | Teacher | Enroll a student in a class |
| DELETE | `/api/students/{studentId}/class` | Teacher | Remove a student from their class |
| GET | `/api/students/search?name=&page=&pageSize=` | Teacher | Search students by name (paginated) |
| GET | `/api/students/{studentId}/details` | Teacher | Full student profile with progress and stats |
| GET | `/api/students/{studentId}/badges` | Teacher | Badges earned by a student |

---

### Books — `api/books`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/books?gradeLevel=` | Teacher | List all books (optional grade filter) |
| POST | `/api/books` | Teacher | Add a new book to the catalog |

---

### Reading — `api/reading`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/reading/{classId}/assigned-books` | Teacher | Assign a book to a class |
| GET | `/api/reading/{classId}/books` | Teacher | List books assigned to a class |

---

### Assignments — `api/classes/{classId}/assignments`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/classes/{classId}/assignments` | Teacher | Create an assignment for a class |
| GET | `/api/classes/{classId}/assignments` | Teacher | List all assignments for a class |

---

### Challenges — `api/classes/{classId}/challenges`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/classes/{classId}/challenges` | Teacher | Create a challenge for a class |
| GET | `/api/classes/{classId}/challenges` | Teacher | List all challenges for a class |

---

### Dashboard — `api/classes/{classId}`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/classes/{classId}/dashboard` | Teacher | Class analytics dashboard |
| GET | `/api/classes/{classId}/students/activity` | Teacher | Today's activity per student |
| GET | `/api/classes/{classId}/leaderboard` | Teacher | Full points leaderboard |

---

### Parent — `api/parent`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/parent/students` | Parent | List the parent's student profiles |
| POST | `/api/parent/students` | Parent | Create a student profile |
| GET | `/api/parent/students/{studentId}` | Parent | Full student details |
| PATCH | `/api/parent/students/{studentId}/reading/{assignedBookId}` | Parent | Update reading progress (current page) |
| PATCH | `/api/parent/students/{studentId}/assignments/{assignmentId}` | Parent | Mark an assignment completed |

---

## Gamification System

### Points
- Reading: points awarded per page read
- Assignment completion: fixed points per assignment

### Streaks
- A streak increments each day the student logs any activity
- The streak resets if there is a gap of more than one day
- Best-streak is tracked independently of the current streak

### Badges
Badges are awarded automatically by the badge evaluation engine after every activity log. The five badge types are seeded on startup with configurable thresholds.

### LearningActivity Engine
Every book assignment, assignment, and challenge creates a corresponding `LearningActivity` row. `StudentLearningActivityProgress` provides a unified view of all activities across all types for a student, accessible from both the teacher's student detail view and the parent's student detail view.

### Challenge Progress
Challenge progress is updated automatically — parents do not need to update it manually. When a student reads pages, the engine increments all active Pages and Books challenges. When an assignment is marked complete, all active Assignments challenges are incremented.

---

## Student Enrollment Flow

1. Parent registers (`POST /api/auth/register-parent`)
2. Parent creates student profile (`POST /api/parent/students`)
3. Teacher searches for student by name (`GET /api/students/search?name=...`)
4. Teacher enrolls student in class (`POST /api/classes/{classId}/students/{studentId}`)
   — All existing books, assignments, challenges, and learning activities are bootstrapped automatically

---

## Author

Zhivko Mihailov
