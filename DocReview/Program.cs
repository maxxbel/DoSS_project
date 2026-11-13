using System.Text;
using DocReview.Api.Middleware;
using DocReview.Infrastructure;
using DocReview.Infrastructure.Auth;
using DocReview.Infrastructure.RequestManagement;
using DocReview.Infrastructure.Security;
using DocReview.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT:Secret is not configured.");
var jwtIssuer = jwtSection["Issuer"] ?? "DocumentReviewApi";
var jwtAudience = jwtSection["Audience"] ?? "DocumentReviewApiClients";
var jwtExpiryMinutes = int.Parse(jwtSection["ExpiryMinutes"] ?? "60");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// database 
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=database.db");
});

// services
builder.Services.AddSingleton<ITokenService>(new JwtService(jwtIssuer, jwtSecret, jwtAudience, jwtExpiryMinutes));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRequestManagementService, RequestManagementService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();