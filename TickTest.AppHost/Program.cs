var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web_TickTest>("web-ticktest");

builder.AddProject<Projects.Web_MarketData>("web-marketdata");

builder.Build().Run();
