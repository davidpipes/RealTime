using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

namespace Api.Endpoints;

public class QueryWorkItemsRequest
{
    public string? LoanIdentifier { get; set; }
    public string? Domain { get; set; }
    public string? Status { get; set; }
}

public class QueryWorkItemsResponse
{
    public long Id { get; set; }
    public string LoanIdentifier { get; set; }
    public string Domain { get; set; }
    public string Status { get; set; }
    public DateTime Modified { get; set; }
}

public class QueryWorkItemsEndpoint : Endpoint<QueryWorkItemsRequest, IEnumerable<QueryWorkItemsResponse>>
{
    private readonly IDbConnectionFactory _db;

    public QueryWorkItemsEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Get("/api/workitems");
        AllowAnonymous();
        Description(x => x.AutoTagOverride("query"));
    }

    public override async Task HandleAsync(QueryWorkItemsRequest req, CancellationToken ct)
    {
        var sql = @"
            SELECT pk AS Id, loan_identifier AS LoanIdentifier, domain, status, modified
            FROM workitems
            WHERE (@LoanIdentifier IS NULL OR loan_identifier = @LoanIdentifier)
              AND (@Domain IS NULL OR domain = @Domain)
              AND (@Status IS NULL OR status = @Status)";

        using var connection = await _db.CreateConnectionAsync();

        var workItems = await connection.QueryAsync<QueryWorkItemsResponse>(
            new CommandDefinition(sql, req, cancellationToken: ct));

        await SendOkAsync(workItems, ct);
    }
}