var builder = DistributedApplication.CreateBuilder(args);

// pgvector image required — EF migrations use the Postgres vector extension.
var postgres = builder
    .AddPostgres("postgres")
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg16")
    .WithDataVolume("taskmanager-aspire-pgdata")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));

// Resource name "MainDb" injects ConnectionStrings__MainDb for WebApi MasterConfig binding.
var mainDb = postgres.AddDatabase("MainDb", databaseName: "taskdb");

// AddProject already registers http/https endpoints from the project; update ports
// instead of calling WithHttp(s)Endpoint (those try to create duplicate names on Aspire 13.1).
var webapi = builder
    .AddProject<Projects.WebApi>("webapi")
    .WithReference(mainDb)
    .WaitFor(mainDb)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5000;
        endpoint.IsExternal = true;
    })
    .WithEndpoint("https", endpoint =>
    {
        endpoint.Port = 5001;
        endpoint.IsExternal = true;
    });

builder
    .AddProject<Projects.WebApp>("webapp")
    .WithEndpoint("https", endpoint =>
    {
        endpoint.Port = 5002;
        endpoint.IsExternal = true;
    })
    .WaitFor(webapi);

builder.Build().Run();
