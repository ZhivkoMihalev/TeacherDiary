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

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (SQL Server) |
| Authentication | JWT Bearer |
| Logging | Serilog (structured, request logging) |
| API docs | Swagger / OpenAPI (XML comments) |

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (local or remote)

### Configuration

Create or update `TeacherDiary.Api/appsettings.json` (never commit secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=TeacherDiary;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "TeacherDiary",
    "Audience": "TeacherDiary",
    "SigningKey": "<at-least-32-character-secret>",
    "ExpiresInMinutes": 1440
  },
  "AllowedCorsOrigins": [
    "http://localhost:5173"
  ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

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
