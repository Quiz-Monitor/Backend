using Microsoft.EntityFrameworkCore;
using QuizMonitor.DAL.Data;

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
    Console.WriteLine($"Warning: .env file not found at {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
// Read from environment variable directly since we loaded it with DotNetEnv
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

builder.Services.AddDbContext<QuizMonitorDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuizMonitor API",
        Version = "v1",
        Description = "Quiz Monitoring System API with Proctoring Features"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseAuthorization();

app.MapControllers();

// Redirect root URL to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
