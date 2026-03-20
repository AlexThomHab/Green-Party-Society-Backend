using System.Text;
using GreenPartySocietyAPI.Data;
using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Repositories;
using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=Data/app.db");
});

// Memory Cache (for blog feed)
builder.Services.AddMemoryCache();

// HTTP Clients
builder.Services.AddHttpClient("substack");
builder.Services.AddHttpClient("instagram");
builder.Services.AddHttpClient("anthropic");

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new Exception("JWT key missing (Jwt:Key)");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireClaim("role", UserRole.Admin));
    options.AddPolicy("CommitteeOrAdmin", p => p.RequireClaim("role", UserRole.Admin, UserRole.Committee));
});

// Application Services
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IInstagramService, InstagramService>();
builder.Services.AddScoped<IAiEventExtractionService, AiEventExtractionService>();
builder.Services.AddScoped<IEventSyncService, EventSyncService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background Services
builder.Services.AddHostedService<EventSyncHostedService>();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
