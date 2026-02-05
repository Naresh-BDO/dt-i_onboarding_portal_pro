using System.Text;
using DT_I_Onboarding_Portal.Data;
using DT_I_Onboarding_Portal.Server.Middleware;
using DT_I_Onboarding_Portal.Core.Models;
using DT_I_Onboarding_Portal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DT_I_Onboarding_Portal.Data.Stores;

var builder = WebApplication.CreateBuilder(args);

// CORS Configuration - Allow frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:64954", "https://localhost:64954", "http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with Bearer Auth
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DT-I Onboarding Portal API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: **Bearer {your token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SMTP Configuration
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("SmtpSettings"));

// JWT Configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var keyString = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(keyString))
{
    throw new InvalidOperationException("Jwt:Key is missing in configuration.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

// Services Registration
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EfUserStore>();

// Build App
var app = builder.Build();

// Middleware Pipeline
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DT-I Onboarding Portal API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<RoleMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

// Database Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db.Database.MigrateAsync();
        Console.WriteLine("✓ Database migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Database migration failed: {ex.Message}");
    }

    var env = services.GetRequiredService<IHostEnvironment>();
    if (env.IsDevelopment())
    {
        var userStore = services.GetRequiredService<EfUserStore>();
        Console.WriteLine("✓ Application initialized with default roles and users");
    }
}

Console.WriteLine("🚀 Application started. API: https://localhost:7107/swagger");
app.Run();
    