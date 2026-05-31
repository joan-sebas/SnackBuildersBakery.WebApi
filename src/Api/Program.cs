using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed the baseline menu and rehydrate the in-memory scheduler from persisted order items.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await MenuItemSeed.SeedAsync(db);

    var reconstruction = scope.ServiceProvider.GetRequiredService<SchedulerReconstructionService>();
    await reconstruction.ReconstructAsync();
}

// UseExceptionHandler without a path + AddProblemDetails produces RFC 7807 responses for
// unhandled exceptions. UseStatusCodePages covers 404 and other non-exception status codes.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.Run();
