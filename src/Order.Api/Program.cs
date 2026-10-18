using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Api.Clients;
using Order.Api.Data;
using Order.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// CONCEPT: STANDARDIZED RFC 7807 ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});

// Built-in OpenAPI document generation (.NET 10)
builder.Services.AddOpenApi();

// Configure EF Core with Microsoft SQL Server
var connectionString = builder.Configuration.GetConnectionString("OrderDb");
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// CONCEPT: TYPED HTTP CLIENT & RESILIENCE PIPELINE
// Registers ICatalogClient backed by IHttpClientFactory socket pooling.
// Attaches AddStandardResilienceHandler (Microsoft.Extensions.Http.Resilience)
// providing bounded retries, attempt timeouts, total request timeouts, and circuit breakers.
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
{
    var catalogUrl = builder.Configuration["Services:CatalogApi"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(catalogUrl);
})
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
});

// Register application services (Scoped = created once per HTTP request)
builder.Services.AddScoped<IOrderService, OrderService>();

// Configure MassTransit with RabbitMQ message broker
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var port = ushort.TryParse(builder.Configuration["RabbitMQ:Port"], out var p) ? p : (ushort)5672;
        var user = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, port, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Centralized error handling pipeline
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
