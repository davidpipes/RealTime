using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

namespace Api.Endpoints;

public class GetWorkItemRequest
{
    public long Id { get; set; }
}

public class GetWorkItemResponse
{
    public long Id { get; set; }
    public string LoanIdentifier { get; set; }
    public string Domain { get; set; }
    public string Status { get; set; }
    public DateTime Modified { get; set; }
}

public class GetWorkItemEndpoint : Endpoint<GetWorkItemRequest, GetWorkItemResponse>
{
    private readonly IDbConnectionFactory _db;

    public GetWorkItemEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Get("/api/workitem/{Id}");
        AllowAnonymous();
        Description(x => x.AutoTagOverride("workitem"));
    }

    public override async Task HandleAsync(GetWorkItemRequest req, CancellationToken ct)
    {
        const string sql = @"
            SELECT pk AS Id, loan_identifier AS LoanIdentifier, domain, status, modified 
            FROM workitems 
            WHERE pk = @Id";

        var response = await (await _db.CreateConnectionAsync()).QuerySingleOrDefaultAsync<GetWorkItemResponse>(
            new CommandDefinition(sql, req, cancellationToken: ct));

        if (response is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(response, ct);
    }
}