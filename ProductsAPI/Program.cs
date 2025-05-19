using WebShopLibrary;
using WebShopLibrary.Database;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// JWT-service
builder.Services.AddSingleton<JwtService>();

var key = Encoding.ASCII.GetBytes("2XjMUiuCS6E06z!j679dGKIMRpK4wmqeL");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// Tilføj connection strings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var logConnectionString = builder.Configuration.GetConnectionString("LogConnection");

// Registrer databaser og services
builder.Services.AddScoped<DBConnection>(_ => new DBConnection(connectionString));
builder.Services.AddScoped<LogDBConnection>(_ => new LogDBConnection(logConnectionString));
builder.Services.AddScoped<LogService>();

// Repository og services med logning
builder.Services.AddScoped<ProductRepositoryDb>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();

// HTTP context (kræves for brugerdata i controllers)
builder.Services.AddHttpContextAccessor();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                      });
});

// Swagger, controller osv.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// CSP headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; object-src 'none'; frame-ancestors 'none'; upgrade-insecure-requests; base-uri 'self'");
    await next();
});

app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
