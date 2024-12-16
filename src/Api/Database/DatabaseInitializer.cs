using Dapper;

namespace Api.Database;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Create the table if it doesn't exist
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS workitems (
                pk BIGSERIAL PRIMARY KEY,
                loan_identifier VARCHAR NOT NULL,
                domain VARCHAR NOT NULL,
                status VARCHAR NOT NULL,
                modified TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ");

        // Create the trigger function if it doesn't exist
        await connection.ExecuteAsync(@"
            CREATE OR REPLACE FUNCTION notify_workitems_change()
            RETURNS TRIGGER AS $$
            BEGIN
                PERFORM pg_notify(
                    'workitems_channel', 
                    json_build_object(
                        'operation', TG_OP,
                        'id', NEW.pk,
                        'loan_identifier', NEW.loan_identifier,
                        'domain', NEW.domain,
                        'status', NEW.status,
                        'modified', NEW.modified
                    )::text
                );
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Create the trigger if it doesn't exist
        await connection.ExecuteAsync(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_trigger
                    WHERE tgname = 'workitems_notify_trigger'
                ) THEN
                    CREATE TRIGGER workitems_notify_trigger
                    AFTER INSERT OR UPDATE OR DELETE ON workitems
                    FOR EACH ROW
                    EXECUTE FUNCTION notify_workitems_change();
                END IF;
            END;
            $$;
        ");
    }
}
