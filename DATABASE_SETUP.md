# Database First Approach - Setup Complete ✅

## Overview

This document describes the database-first setup completed for the QuizMonitor application using Entity Framework Core and PostgreSQL (Supabase).

## What Was Done

### 1. **Scaffolded Database Entities** ✅

Used EF Core's database-first approach to generate C# entities from the existing PostgreSQL database.

**Command used:**
```bash
cd QuizMonitor.DAL
dotnet ef dbcontext scaffold \
  "Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.lbjzmajvqaguunipdfie;Password=QbGwHcmnkmWI6TUn" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --output-dir Models \
  --context-dir Data \
  --context QuizMonitorDbContext \
  --force \
  --no-onconfiguring \
  --table user --table exam --table question --table choice \
  --table exam_attempt --table question_answer --table violation_event \
  --table answer_violation --table notification --table user_notification
```

### 2. **Generated Files**

#### **Models** (in `QuizMonitor.DAL/Models/`)
- `User.cs` - User entity with roles (instructor, student, admin)
- `Exam.cs` - Exam configuration and settings
- `Question.cs` - Exam questions (MCQ, open-ended)
- `Choice.cs` - Multiple choice options
- `ExamAttempt.cs` - Student exam attempts with scores
- `QuestionAnswer.cs` - Individual question answers
- `ViolationEvent.cs` - Cheating detection events
- `AnswerViolation.cs` - Junction table linking answers to violations
- `Notification.cs` - System notifications
- `UserNotification.cs` - User-specific notification delivery status

#### **DbContext** (in `QuizMonitor.DAL/Data/`)
- `QuizMonitorDbContext.cs` - EF Core DbContext with all entity configurations

### 3. **Configured Connection String**

**Updated `.env` file:**
```env
ConnectionStrings__DefaultConnection=Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.lbjzmajvqaguunipdfie;Password=QbGwHcmnkmWI6TUn
```

**Note:** The connection string format was changed from PostgreSQL URI format to Npgsql standard format for compatibility.

### 4. **Configured Dependency Injection**

**Updated `Program.cs`:**
```csharp
using Microsoft.EntityFrameworkCore;
using QuizMonitor.DAL.Data;

// Load .env file from the solution root (parent directory)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
builder.Services.AddDbContext<QuizMonitorDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 5. **Created Health Check Endpoint**

Added `HealthController.cs` with two endpoints:

1. **GET `/api/health`** - Basic API health check
2. **GET `/api/health/db`** - Database connection and statistics

**Example response:**
```json
{
  "status": "healthy",
  "database": "connected",
  "statistics": {
    "users": 0,
    "exams": 0
  },
  "timestamp": "2026-01-10T01:05:30.3402845Z"
}
```

### 6. **Updated Project References**

**QuizMonitor.API.csproj:**
```xml
<ItemGroup>
  <PackageReference Include="DotNetEnv" Version="3.1.1" />
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.4" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\QuizMonitor.BLL\QuizMonitor.BLL.csproj" />
  <ProjectReference Include="..\QuizMonitor.DAL\QuizMonitor.DAL.csproj" />
</ItemGroup>
```

**QuizMonitor.DAL.csproj:**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
</ItemGroup>
```

## Database Schema

The scaffolded entities match your DDL schema with the following tables:

| Table | Purpose |
|-------|---------|
| `user` | User accounts (instructors, students, admins) |
| `exam` | Exam configurations and settings |
| `question` | Exam questions (MCQ single/multiple, open-ended) |
| `choice` | Answer choices for MCQ questions |
| `exam_attempt` | Student exam attempts with scoring |
| `question_answer` | Individual answers per question |
| `violation_event` | Cheating detection events |
| `answer_violation` | Links violations to specific answers |
| `notification` | System notifications |
| `user_notification` | User-specific notification delivery |

## Key Features

✅ **Soft Deletes**: All entities support soft deletion with `deleted_at` and `deleted_by` fields  
✅ **Audit Trails**: Automatic `created_at` and `updated_at` timestamps  
✅ **Relationships**: All foreign key relationships properly mapped  
✅ **Proctoring**: Built-in support for cheating detection metrics  
✅ **Role-Based Access**: User roles (instructor, student, admin) enforced at DB level  

## Testing the Connection

### 1. Start the API
```bash
cd QuizMonitor.API
dotnet run --urls "http://localhost:5000"
```

### 2. Test Database Connection
```bash
curl http://localhost:5000/api/health/db
```

Expected output:
```json
{
  "status": "healthy",
  "database": "connected",
  "statistics": {
    "users": 0,
    "exams": 0
  },
  "timestamp": "2026-01-10T..."
}
```

## Next Steps

### 1. **Create Repositories** (Recommended)
Implement the Repository pattern in `QuizMonitor.DAL/Repositories/`:
- `IUserRepository` / `UserRepository`
- `IExamRepository` / `ExamRepository`
- `IExamAttemptRepository` / `ExamAttemptRepository`
- etc.

### 2. **Implement Business Logic**
Create services in `QuizMonitor.BLL/Services/`:
- `AuthService` - Registration, login, JWT tokens
- `ExamService` - Exam CRUD operations
- `GradingService` - Automated and manual grading
- `ViolationService` - Cheating detection handling
- `NotificationService` - Push notifications

### 3. **Create DTOs**
Define Data Transfer Objects in `QuizMonitor.BLL/DTOs/`:
- Request DTOs (for API input)
- Response DTOs (for API output)
- Mapping profiles (using AutoMapper)

### 4. **Implement Controllers**
Create API controllers following the design in `api_design.md`:
- `AuthController` - Registration, login
- `ExamsController` - Exam management (instructor)
- `ExamAttemptsController` - Exam taking (student)
- `GradingController` - Manual grading (instructor)
- `NotificationsController` - User notifications

### 5. **Add Authentication & Authorization**
- Implement JWT authentication
- Add role-based authorization policies
- Secure endpoints based on user roles

### 6. **Write Tests**
Create unit and integration tests in `QuizMonitor.Tests/`:
- Repository tests
- Service tests
- Controller tests
- Integration tests

## Important Notes

⚠️ **Security:**
- The `.env` file is in `.gitignore` - never commit it!
- Rotate the JWT secret key and database password regularly
- Use HTTPS in production

⚠️ **Connection String:**
- The connection string is loaded from `.env` using DotNetEnv
- Make sure the `.env` file is in the solution root (one level up from QuizMonitor.API)

⚠️ **Scaffolding:**
- The `--no-onconfiguring` flag was used to prevent hardcoded connection strings in DbContext
- Re-scaffolding will overwrite existing entity files (use `--force` carefully)

## Troubleshooting

### Problem: "Cannot connect to database"
**Solution:** Check that:
1. `.env` file exists in the solution root
2. Connection string is in Npgsql format (not PostgreSQL URI)
3. Supabase database is accessible
4. Database credentials are correct

### Problem: "Connection string is null or empty"
**Solution:** Ensure DotNetEnv loads the .env file from the correct path:
```csharp
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
DotNetEnv.Env.Load(envPath);
```

### Problem: Build errors with 'Object' type
**Solution:** This happens when Supabase storage tables are scaffolded. Use the `--table` flag to scaffold only your application tables.

## Resources

- [EF Core Database First](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [Supabase Documentation](https://supabase.com/docs)

---

**Status:** ✅ Database setup complete and tested successfully!

**Date:** January 10, 2026
