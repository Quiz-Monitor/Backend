# Quiz Monitor - Backend

An N-Tier ASP.NET Core Web API for the Quiz Monitor system with PostgreSQL database.

## Project Structure

```
QuizMonitor/
├── QuizMonitor.API/          # Web API Layer (Controllers, Middleware)
├── QuizMonitor.BLL/          # Business Logic Layer (Services, DTOs)
├── QuizMonitor.DAL/          # Data Access Layer (Models, Repositories, DbContext)
└── QuizMonitor.Tests/        # Unit and Integration Tests
```

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL Database (Supabase)
- IDE: Visual Studio, VS Code, or Rider

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Graduation_Project
```

### 2. Configure Environment Variables

1. Copy the `.env.example` file to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and replace the placeholder values:
   ```env
   ConnectionStrings__DefaultConnection=postgresql://postgres:YOUR-ACTUAL-PASSWORD@db.lbjzmajvqaguunipdfie.supabase.co:5432/postgres
   JwtSettings__SecretKey=your-super-secret-key-minimum-32-characters-long
   ```

   **⚠️ IMPORTANT:** 
   - Never commit the `.env` file to version control
   - Keep your database password and JWT secret secure
   - The `.env` file is already in `.gitignore`

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Run the API

```bash
cd QuizMonitor.API
dotnet run
```

The API will automatically open Swagger UI in your browser at:
- **HTTPS (Default):** `https://localhost:7158/swagger`
- **HTTP:** `http://localhost:5149/swagger`

You can also access the root URL which will redirect to Swagger:
- `https://localhost:7158/` → redirects to Swagger UI
- `http://localhost:5149/` → redirects to Swagger UI

**Note:** The ports (7158 for HTTPS, 5149 for HTTP) are configured in `Properties/launchSettings.json`.

## Environment Variables

The application uses the following environment variables (defined in `.env`):

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `postgresql://user:pass@host:5432/db` |
| `JwtSettings__SecretKey` | Secret key for JWT token generation | `your-super-secret-key` |
| `JwtSettings__Issuer` | JWT token issuer | `QuizMonitor` |
| `JwtSettings__Audience` | JWT token audience | `QuizMonitorUsers` |
| `JwtSettings__ExpirationMinutes` | Access token expiration time | `60` |
| `JwtSettings__RefreshTokenExpirationDays` | Refresh token expiration time | `7` |

## Database Setup

The project uses PostgreSQL with Entity Framework Core. Connection details:
- **Provider:** Npgsql.EntityFrameworkCore.PostgreSQL 9.0
- **Host:** Supabase
- **ORM:** Entity Framework Core 9.0

### Run Migrations (Coming Soon)

```bash
cd QuizMonitor.API
dotnet ef migrations add InitialCreate --project ../QuizMonitor.DAL
dotnet ef database update
```

## API Documentation

Once the API is running, Swagger UI will open automatically. You can also manually access:
- **Swagger UI:** `https://localhost:7158/swagger` (Development mode)
- **API Design:** See `api_design.md` for detailed endpoint specifications

The Swagger UI provides interactive documentation where you can test all endpoints directly from your browser.

## Project Layers

### 1. QuizMonitor.API (Presentation Layer)
- Controllers
- Middleware
- Request/Response models
- Dependency injection configuration

### 2. QuizMonitor.BLL (Business Logic Layer)
- Services (business logic)
- DTOs (Data Transfer Objects)
- Validation logic
- Business rules

### 3. QuizMonitor.DAL (Data Access Layer)
- Entity models
- DbContext
- Repositories
- Database migrations

### 4. QuizMonitor.Tests
- Unit tests
- Integration tests
- Test fixtures

## Development

### Running Tests

```bash
dotnet test
```

### Code Structure Guidelines

- Follow the N-Tier architecture pattern
- Keep business logic in the BLL layer
- Database access only through repositories in DAL
- Controllers should be thin, delegating to services

## Security Notes

 **Important Security Practices:**

1. **Never commit `.env` files** - They contain sensitive credentials
2. **Use strong JWT secrets** - Minimum 32 characters, random
3. **Rotate secrets regularly** - Change passwords and keys periodically
4. **Use HTTPS in production** - Always encrypt traffic
5. **Validate all inputs** - Prevent SQL injection and XSS attacks

## Contributing

1. Create a feature branch
2. Make your changes
3. Write/update tests
4. Submit a pull request







