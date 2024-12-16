using Api.Database;

using Dapper;

using FastEndpoints;
using FastEndpoints.Swagger;

namespace Api.Endpoints;

public class CreateWorkItemRequest
{
    public string LoanIdentifier { get; set; }
    public string Domain { get; set; }
    public string Status { get; set; }
}

public class CreateWorkItemResponse
{
    public long Id { get; set; }
}

public class CreateWorkItemEndpoint : Endpoint<CreateWorkItemRequest, CreateWorkItemResponse>
{
    private readonly IDbConnectionFactory _db;

    public CreateWorkItemEndpoint(IDbConnectionFactory db) => _db = db;

    public override void Configure()
    {
        Post("/api/workitem");
        AllowAnonymous();
        Description(x => x.AutoTagOverride("workitem"));
    }

    public override async Task HandleAsync(CreateWorkItemRequest req, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO workitems (loan_identifier, domain, status, modified) 
            VALUES (@LoanIdentifier, @Domain, @Status, CURRENT_TIMESTAMP)
            RETURNING pk AS Id";

        var response = await (await _db.CreateConnectionAsync()).QuerySingleAsync<CreateWorkItemResponse>(
            new CommandDefinition(sql, req, cancellationToken: ct));

        await SendOkAsync(response);
    }
}