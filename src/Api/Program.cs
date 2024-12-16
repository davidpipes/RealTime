using Api.Database;
using Api.Extensions;

using FastEndpoints;
using FastEndpoints.Swagger;

var bld = WebApplication.CreateBuilder();

// Configure database
bld.Services.ConfigureDatabase(bld.Configuration);

// Add FastEndpoints and Swagger
bld.Services
   .AddFastEndpoints()
   .SwaggerDocument();

// Add CORS policy
bld.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = bld.Build();

// Middleware configuration
app.UseCors();              // Add CORS middleware here
app.UseFastEndpoints();     // Ensure this comes after UseCors
app.UseSwaggerGen();        // Add Swagger generation middleware

// Initialize database
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
