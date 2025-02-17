using WebShopLibrary;
using WebShopLibrary.Database;


var builder = WebApplication.CreateBuilder(args);

// Henter connection string fra appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrerer DBConnection og WatchRepository som scoped services til dependency injection
builder.Services.AddScoped<DBConnection>(provider => new DBConnection(connectionString));
builder.Services.AddScoped<ProductRepositoryDb>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                      });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Tilføjer HSTS med specifikke indstillinger
builder.Services.AddHsts(options =>
{
    options.Preload = true; // Forhåndsindlæser HSTS til browsere
    options.IncludeSubDomains = true; // Gælder for alle subdomæner
    options.MaxAge = TimeSpan.FromDays(365); // Varighed på 365 dage
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Tilføjer Content-Security-Policy i både udvikling og produktion, så sikkerhed kan testes
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; object-src 'none'; frame-ancestors 'none'; upgrade-insecure-requests; base-uri 'self'"); // CSP beskytter mod XSS og andre angreb
    await next();
});

app.UseHsts(); // Aktiverer HSTS middleware altid, så det kan testes i både udvikling og produktion

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();