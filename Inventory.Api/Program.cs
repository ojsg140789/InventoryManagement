using System.Text.Json.Serialization;
using Inventory.Api.Errors;
using Inventory.Api.Health;
using Inventory.Application.Features.Products;
using Inventory.Application.Features.Warehouses;
using Inventory.Application.Features.Stocks;
using Inventory.Application.Features.StockMovements;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// This MVP has no authentication. Fail closed outside local development.
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This unauthenticated MVP runs only in Development. Add authentication and authorization before deployment.");

var connectionString = builder.Configuration.GetConnectionString("Inventory");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings__Inventory using environment variables or external configuration.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<CreateProductUseCase>();
builder.Services.AddScoped<GetProductsUseCase>();
builder.Services.AddScoped<GetProductByIdUseCase>();
builder.Services.AddScoped<CreateWarehouseUseCase>();
builder.Services.AddScoped<GetWarehousesUseCase>();
builder.Services.AddScoped<GetStockUseCase>();
builder.Services.AddScoped<RegisterStockEntryUseCase>();
builder.Services.AddScoped<RegisterStockOutputUseCase>();
builder.Services.AddScoped<GetStockMovementsUseCase>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sqlserver", tags: ["ready"]);

var app = builder.Build();
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.Run();
