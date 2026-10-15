using Microsoft.EntityFrameworkCore;
using Order.Api.Data;

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
