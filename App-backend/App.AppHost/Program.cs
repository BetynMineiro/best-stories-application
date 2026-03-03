var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.App_Api>("App-Api")
    .WithHttpHealthCheck("/health");

builder.Build().Run();