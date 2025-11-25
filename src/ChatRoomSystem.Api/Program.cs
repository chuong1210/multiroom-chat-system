using System.Text;
using ChatRoomSystem.Api.Middleware;
using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var configuration = builder.Configuration;

// ===== Database =====
builder.Services.AddDbContext<ChatRoomDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=chatroom.db";
    options.UseSqlite(connectionString);
});

// ===== Identity =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ChatRoomDbContext>()
.AddDefaultTokenProviders();

// ===== JWT Authentication =====
var secretKey = configuration["JwtSettings:SecretKey"]
    ?? "YourSuperSecretKeyHereShouldBeAtLeast32CharactersLong!";
var issuer = configuration["JwtSettings:Issuer"] ?? "ChatRoomSystem";
var audience = configuration["JwtSettings:Audience"] ?? "ChatRoomSystem";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===== Services =====
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddScoped<WebSocketMessageHandler>();
builder.Services.AddScoped<JwtService>();

// ===== Controllers =====
builder.Services.AddControllers();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Chat Room System API",
        Version = "v1",
        Description = "Multi-room chat system with real-time WebSocket communication"
    });

    // JWT Bearer auth trong Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== Database Migration =====
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error migrating database: {ex.Message}");
    }
}

// ===== Middleware Pipeline =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Room System API v1");
        c.RoutePrefix = string.Empty; // Swagger UI tại root URL
    });
}

app.UseCors("AllowAll");

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Custom WebSocket middleware
app.UseMiddleware<WebSocketMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

Console.WriteLine("=================================================");
Console.WriteLine("  Chat Room System API");
Console.WriteLine("=================================================");
Console.WriteLine($"  Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"  API URL: http://localhost:5000");
Console.WriteLine($"  Swagger UI: http://localhost:5000");
Console.WriteLine($"  WebSocket: ws://localhost:5000/ws?token=<jwt>");
Console.WriteLine("=================================================");

app.Run();
