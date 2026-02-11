using Microsoft.EntityFrameworkCore;
using QuizMonitor.DAL.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuizMonitor.BLL.Services;
using QuizMonitor.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Repositories;

// Enable legacy timestamp behavior for PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Load .env file from the solution root (parent directory)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine($".env file not found at: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
// Read from environment variable directly since we loaded it with DotNetEnv
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

builder.Services.AddDbContext<QuizMonitorDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories and Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IExamAttemptService, ExamAttemptService>();

// Add controllers
builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSecretKey = Environment.GetEnvironmentVariable("JwtSettings__SecretKey");
var jwtIssuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer");
var jwtAudience = Environment.GetEnvironmentVariable("JwtSettings__Audience");

if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured in environment variables");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero,
        // IMPORTANT: Configure role claim type to match what we use in JWT
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

// Add authorization with case-insensitive role matching
builder.Services.AddAuthorization(options =>
{
    // Define policies with case-insensitive role checking
    options.AddPolicy("InstructorPolicy", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("instructor") || 
            context.User.IsInRole("Instructor")));
    
    options.AddPolicy("StudentPolicy", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("student") || 
            context.User.IsInRole("Student")));
    
    options.AddPolicy("AdminPolicy", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("admin") || 
            context.User.IsInRole("Admin")));
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuizMonitor API",
        Version = "v1",
        Description = "Quiz Monitoring System API with Proctoring Features and JWT Authentication"
    });

    // Add JWT authentication to Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizMonitor API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();