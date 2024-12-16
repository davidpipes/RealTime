using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

namespace Api.Endpoints;

public class UpdateWorkItemRequest
{
    public long Id { get; set; }
    public string LoanIdentifier { get; set; }
    public string Domain { get; set; }
    public string Status { get; set; }
}

public class UpdateWorkItemResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class UpdateWorkItemEndpoint : Endpoint<UpdateWorkItemRequest, UpdateWorkItemResponse>
{
    private readonly IDbConnectionFactory _db;

    public UpdateWorkItemEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Put("/api/workitem");
        AllowAnonymous();
        Description(x => x.AutoTagOverride("workitem"));
    }

    public override async Task HandleAsync(UpdateWorkItemRequest req, CancellationToken ct)
    {
        const string sql = @"
            UPDATE workitems
            SET loan_identifier = @LoanIdentifier,
                domain = @Domain,
                status = @Status,
                modified = CURRENT_TIMESTAMP
            WHERE pk = @Id";

        using var connection = await _db.CreateConnectionAsync();

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(sql, req, cancellationToken: ct));

        if (rowsAffected > 0)
        {
            await SendOkAsync(new UpdateWorkItemResponse
            {
                Success = true,
                Message = "Work item successfully updated."
            }, ct);
        }
        else
        {
            await SendNotFoundAsync(ct);
        }
    }
}