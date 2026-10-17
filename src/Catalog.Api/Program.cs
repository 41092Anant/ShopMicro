using Catalog.Api.Data;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// CONCEPT: STANDARDIZED RFC 7807 ProblemDetails
// ProblemDetails provides a machine-readable format for HTTP API errors (RFC 7807).
// Adding AddProblemDetails() registers IProblemDetailsService, ensuring consistent error structures
// across validation failures, empty status codes, and unhandled exceptions.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});

// CONCEPT: OPENAPI DOCUMENTATION (.NET 10)
// ASP.NET Core provides built-in OpenAPI document generation without external third-party dependencies.
builder.Services.AddOpenApi();

// Configure EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("CatalogDb");
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Configure Redis distributed cache (IDistributedCache)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "Catalog_";
});

// Register application service dependencies (Scoped = created once per HTTP request)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var app = builder.Build();

// Centralized error handling pipeline:
// 1. UseExceptionHandler catches unhandled exceptions and formats them as RFC 7807 ProblemDetails (HTTP 500).
// 2. UseStatusCodePages transforms empty error status codes (e.g. 404 for unknown routes) into RFC 7807 ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
