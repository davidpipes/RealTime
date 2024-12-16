using System.Runtime.CompilerServices;

using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

using Npgsql;

namespace Api.Endpoints;


public class ListenChannelRequest
{
    public string Channel { get; set; }
}

public class ListenEndpoint : Endpoint<ListenChannelRequest>
{

    private readonly IDbConnectionFactory _db;

    public ListenEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Get("/api/listen");
        AllowAnonymous();
        Options(x => x.RequireCors(p => p.AllowAnyOrigin()));
        Description(x => x.AutoTagOverride("listen"));
    }

    public override async Task HandleAsync(ListenChannelRequest req, CancellationToken ct)
    {
        // Determine the event stream based on the requested channel
        IAsyncEnumerable<object> dataStream = req.Channel switch
        {
            "workitems_channel" => ListenForNotifications(ct),
            "random_channel" => GetDataStream(ct),
            _ => throw new ArgumentException($"Unknown channel: {req.Channel}")
        };

        // Send the event stream
        await SendEventStreamAsync(req.Channel, dataStream, ct);
    }

    private async IAsyncEnumerable<object> GetDataStream([EnumeratorCancellation] CancellationToken ct)
    {
        // Immediate acknowledgment to notify the client that the connection is established
        yield return new { message = "Connection established", timestamp = DateTime.UtcNow };

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000);
            yield return new { guid = Guid.NewGuid() };
        }
    }

    private async IAsyncEnumerable<object> ListenForNotifications([EnumeratorCancellation] CancellationToken ct)
    {
        await using var connection = await _db.CreateConnectionAsync() as NpgsqlConnection;

        if (connection == null)
            throw new InvalidOperationException("Failed to create a valid PostgreSQL connection.");

        // Listen to the workitems_channel
        await connection.ExecuteAsync("LISTEN workitems_channel;");

        yield return new { message = "Connection established", channel = "workitems_channel", timestamp = DateTime.UtcNow };

        var notificationQueue = new Queue<object>();
        var notificationEvent = new SemaphoreSlim(0);

        // Subscribe to notifications from PostgreSQL
        connection.Notification += (_, args) =>
        {
            var notification = new
            {
                channel = args.Channel,
                payload = args.Payload,
                timestamp = DateTime.UtcNow
            };

            lock (notificationQueue)
            {
                notificationQueue.Enqueue(notification);
            }
            notificationEvent.Release();
        };

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for PostgreSQL notifications or keep-alive timeout
                var waitTask = connection.WaitAsync(ct);
                var keepAliveTask = Task.Delay(TimeSpan.FromSeconds(10), ct);

                // Wait for the first task to complete
                await Task.WhenAny(waitTask, keepAliveTask);

                // Process notifications from the queue
                lock (notificationQueue)
                {
                    while (notificationQueue.Count > 0)
                    {
                        yield return notificationQueue.Dequeue();
                    }
                }

                // If keep-alive, send a no-op query to keep the connection active
                if (keepAliveTask.IsCompleted)
                {
                    await connection.ExecuteAsync("SELECT 1;", ct);
                }
            }
        }
        finally
        {
            // Stop listening to the channel when the connection is terminated
            await connection.ExecuteAsync("UNLISTEN workitems_channel;");
        }
    }

}