using Catalog.Api.Data;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("CatalogDb");
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register application service dependencies (Scoped = created once per HTTP request)
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
