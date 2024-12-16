using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

namespace Api.Endpoints;

public class DeleteWorkItemRequest
{
    public long Id { get; set; }
}

public class DeleteWorkItemResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class DeleteWorkItemEndpoint : Endpoint<DeleteWorkItemRequest, DeleteWorkItemResponse>
{
    private readonly IDbConnectionFactory _db;

    public DeleteWorkItemEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Delete("/api/workitem/{Id}");
        AllowAnonymous();
        Description(x => x.AutoTagOverride("workitem"));
    }

    public override async Task HandleAsync(DeleteWorkItemRequest req, CancellationToken ct)
    {
        const string sql = @"DELETE FROM workitems WHERE pk = @Id";

        using var connection = await _db.CreateConnectionAsync();

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(sql, req, cancellationToken: ct));

        if (rowsAffected > 0)
        {
            await SendOkAsync(new DeleteWorkItemResponse
            {
                Success = true,
                Message = "Work item successfully deleted."
            }, ct);
        }
        else
        {
            await SendNotFoundAsync(ct);
        }
    }
}