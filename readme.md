\# TeacherDiary



TeacherDiary is a classroom activity tracking platform designed to help teachers monitor student progress, encourage engagement, and provide insights into classroom performance through gamification.



The platform tracks reading progress, assignments, challenges, and student activity while rewarding engagement through points, streaks, badges, and leaderboards.



---



\# Features



\### Teacher

\- Manage classes and students

\- Assign books to classes

\- Create assignments

\- Create classroom challenges

\- Monitor student activity

\- View analytics dashboard

\- Track reading progress and assignments

\- View leaderboards and achievements



\### Parent

\- View children's learning progress

\- Track reading activity

\- Track assignment completion

\- Update reading progress

\- Mark assignments as completed



\### Gamification

\- Points system

\- Student streaks

\- Achievement badges

\- Classroom leaderboards

\- Activity tracking



---



\# Architecture



The backend follows \*\*Clean Architecture principles\*\*.


TeacherDiary

│

├── TeacherDiary.Api

├── TeacherDiary.Application

├── TeacherDiary.Infrastructure

├── TeacherDiary.Domain



\### Layers



\*\*Domain\*\*

\- Entities

\- Enums

\- Core business models



\*\*Application\*\*

\- DTOs

\- Interfaces

\- Application logic



\*\*Infrastructure\*\*

\- EF Core

\- Database access

\- Services implementation



\*\*API\*\*

\- Controllers

\- Authentication

\- Endpoints



---



\# Technology Stack



\### Backend

\- .NET 9

\- ASP.NET Core Web API

\- Entity Framework Core

\- JWT Authentication

\- SQL Server



\### Architecture

\- Clean Architecture

\- Dependency Injection

\- Repository pattern (through DbContext)

\- Service layer



\### Tools

\- Swagger / OpenAPI

\- Git

\- GitHub



---



\# API Overview



Main API modules:



\### Authentication
### Classes
### Students
### Reading
### Assignments
### Challenges
### Dashboard
### Parent


---



\# Future Improvements



\- React frontend application

\- Real-time classroom activity tracking

\- Charts and analytics dashboard

\- Notification system

\- Mobile support



---



\# Author



Zhivko Mihalev




