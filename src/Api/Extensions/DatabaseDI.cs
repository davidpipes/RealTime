using Api.Database;

namespace Api.Extensions;

public static class DatabaseDI
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Retrieve environment variables for database connection
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new InvalidOperationException("DB_HOST is not set.");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"; // Default to 5432 if not set
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? throw new InvalidOperationException("DB_NAME is not set.");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? throw new InvalidOperationException("DB_USER is not set.");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD is not set.");

        // Construct the connection string
        var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

        // Register the IDbConnectionFactory with the constructed connection string
        services.AddSingleton<IDbConnectionFactory>(_ => new PostgresConnectionFactory(connectionString));

        // Register the DatabaseInitializer
        services.AddSingleton<DatabaseInitializer>();

        return services; // Enable method chaining if needed
    }
}