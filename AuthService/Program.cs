using AuthService.Data;
using AuthService.Middleware;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Settings;
using System.Text;

//create app
var builder = WebApplication.CreateBuilder(args);

// 1. Configure Database Context - register dbcontext in DI
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configure JWT Settings
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

// 3. Configure Authentication & JWT Bearer
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
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
        //No grace period after expiry.
    };
});

//support [Authorize] 
builder.Services.AddAuthorization();

// 4. Register Repositories and Services (Dependency Injection)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();

// 5. Add Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 6. Configure Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Railway Reservation System - Auth Service API",
        Version = "v1",
        Description = "Authentication and User Management Service"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// 7. Auto-Seed Database - insert default data 
using (var scope = app.Services.CreateScope())
{
    //temp service scope create - scoped service(AuthDbCOntext) use during startup app
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try
    {
        AuthDbSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        //Get logging service - To write message to terminal(log file)
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not seed Auth database on startup. Ensure SQL Server is accessible.");
    }
}

// 8. Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1");
    });
}

//Middleware in between request pipeline
//Catch every unhandled exception - returns ugly server error.
app.UseMiddleware<GlobalExceptionMiddleware>();

//Checks JWT Tokens.
app.UseAuthentication();
app.UseAuthorization();

//Connects Controller Routes to URLs. [HttpPost("login")] -> /api/auth/login
app.MapControllers();

//Starts Kestrel Web Server.
//Starts listening for requests.(authservice is live)
app.Run();
