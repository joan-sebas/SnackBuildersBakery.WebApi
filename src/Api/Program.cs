using Api.Auth;
using Api.Endpoints;
using Api.ErrorHandling;
using Api.Health;
using Api.Metrics;
using Api.OpenApi;
using Api.Scheduler;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;
using System.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddRoleAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();
builder.Services.AddExceptionHandler<ProblemDetailsHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>());
builder.Services.AddSnackBuildersMetrics();
builder.Services.AddApiHealthChecks();

builder.Services.Configure<KitchenWorkerOptions>(builder.Configuration.GetSection(KitchenWorkerOptions.SectionName));
// The reconciliation worker advances time-based scheduler state; the Testing host drives the
// clock from tests instead, so it is not registered there.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<KitchenReconciliationWorker>();

// Serialize enums as strings for a self-describing API contract.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Seed the baseline menu and rehydrate the in-memory scheduler from persisted order items.
// Skipped in the Testing environment so integration tests do not require a running database.
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await MenuItemSeed.SeedAsync(db);

    var reconstruction = scope.ServiceProvider.GetRequiredService<SchedulerReconstructionService>();
    await reconstruction.ReconstructAsync();
}

// UseExceptionHandler without a path + AddProblemDetails produces RFC 7807 responses for
// unhandled exceptions. UseStatusCodePages covers 404 and other non-exception status codes.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Snack Builders API"));
}

app.MapMenuEndpoints();
app.MapOrderEndpoints();
app.MapPaymentEndpoints();
app.MapKitchenEndpoints();
app.MapApiHealthChecks();

app.Run();

public partial class Program;
