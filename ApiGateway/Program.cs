using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Settings;
using System.Text;

//create,config asp.net core app
var builder = WebApplication.CreateBuilder(args);   //creates ASP.NET application

// 1. Configure JWT Settings
//reads jwt settings(json) from appsettings.json
var jwtSection = builder.Configuration.GetSection("JwtSettings");
//register jwt setting in DI
builder.Services.Configure<JwtSettings>(jwtSection);    //use JWT for login verification
//convert json in c# obj(map to obj)
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

// 2. Configure JWT Authentication
//before access know who are u?
builder.Services.AddAuthentication(options =>
{
    //Whenever authorization is required, check JWT token.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    
})

//How to validate JWT token
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings.Issuer,
        ValidAudience            = jwtSettings.Audience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew                = TimeSpan.Zero
        //default asp.net-5min(extra) but here expire exact time
    };
});

//allowed or not?
builder.Services.AddAuthorization();

// 3. Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 4. Configure HttpClient for proxying downstream Swagger JSON
builder.Services.AddHttpClient();

// 5. Configure Swagger / OpenAPI for ApiGateway
//allow api to appear
builder.Services.AddEndpointsApiExplorer();
//gen swagger doc
builder.Services.AddSwaggerGen(c =>
{
    //Create doc metadata display : RRS-API Gateway
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Railway Reservation System - API Gateway",
        Version     = "v1",
        Description = "Central API Gateway powered by YARP. Use the definition selector at top right to test Auth, Train, Reservation, and Payment microservices directly from this Gateway UI."
    });

    //Adds authorize button in swagger (paste bearer token)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    //Make swagger send token automatic with request
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. Swagger UI aggregation endpoints
// Proxy the downstream swagger json files through gateway port 7000
//To show all microservice APIs from a single Gateway Swagger page.
app.MapGet("/swagger-docs/auth", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    try
    {
        var response = await client.GetAsync("http://localhost:7001/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json");
    }
    catch
    {
        return Results.Problem("AuthService is offline or unreachable at http://localhost:7001", statusCode: 503);
    }
}).ExcludeFromDescription();

app.MapGet("/swagger-docs/trains", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    try
    {
        var response = await client.GetAsync("http://localhost:7002/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json");
    }
    catch
    {
        return Results.Problem("TrainService is offline or unreachable at http://localhost:7002", statusCode: 503);
    }
}).ExcludeFromDescription();

app.MapGet("/swagger-docs/reservations", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    try
    {
        var response = await client.GetAsync("http://localhost:7003/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json");
    }
    catch
    {
        return Results.Problem("ReservationService is offline or unreachable at http://localhost:7003", statusCode: 503);
    }
}).ExcludeFromDescription();

app.MapGet("/swagger-docs/payments", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    try
    {
        var response = await client.GetAsync("http://localhost:7004/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json");
    }
    catch
    {
        return Results.Problem("PaymentService is offline or unreachable at http://localhost:7004", statusCode: 503);
    }
}).ExcludeFromDescription();

// 7. Configure Swagger UI with all service definitions
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //Adds multiple dropdown entries:
    app.UseSwaggerUI(c =>
    {
        // Select definitions from the dropdown at the top right of the Gateway Swagger UI
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway (Central)");
        c.SwaggerEndpoint("/swagger-docs/auth", "Auth Service (Users & JWT)");
        c.SwaggerEndpoint("/swagger-docs/trains", "Train Service (Schedules & Seats)");
        c.SwaggerEndpoint("/swagger-docs/reservations", "Reservation Service (Booking & PNR)");
        c.SwaggerEndpoint("/swagger-docs/payments", "Payment Service (Simulated Payments)");

        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Railway Reservation System - Swagger Gateway";
    });
}

//Checks JWT token.
app.UseAuthentication();
app.UseAuthorization();

// 8. YARP Reverse Proxy handles routing of all incoming API requests
app.MapReverseProxy();

app.Run();
