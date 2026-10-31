using Catalog.Api.Data;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog structured logging with Console and Seq sinks
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        configuration.WriteTo.Seq(seqUrl);
    }
});

// Configure OpenTelemetry distributed tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Catalog.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Npgsql")
            .AddOtlpExporter(options =>
            {
                var endpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:5341/ingest/otlp/v1/traces";
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
    });

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

// Configure Redis distributed cache (IDistributedCache) and connection multiplexer
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 2000;
    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "Catalog_";
});

// Register application service dependencies (Scoped = created once per HTTP request)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Configure health checks (Liveness: process healthy, Readiness: PostgreSQL and Redis reachable)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<CatalogDbContext>("catalog-db", tags: ["ready"])
    .AddCheck<Catalog.Api.Health.RedisHealthCheck>("redis", tags: ["ready"]);

var app = builder.Build();

// Centralized error handling pipeline:
// 1. UseExceptionHandler catches unhandled exceptions and formats them as RFC 7807 ProblemDetails (HTTP 500).
// 2. UseStatusCodePages transforms empty error status codes (e.g. 404 for unknown routes) into RFC 7807 ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Clean HTTP request logging with Serilog
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Map health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

app.MapControllers();

static Task WriteHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.ToString(),
        checks = report.Entries.Select(e => new
        {
            component = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.ToString(),
            error = e.Value.Exception?.Message
        })
    };
    return context.Response.WriteAsJsonAsync(response);
}

app.Run();
