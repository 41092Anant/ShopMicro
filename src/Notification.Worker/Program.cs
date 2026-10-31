using MassTransit;
using Notification.Worker.Consumers;
using Notification.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog structured logging
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Register idempotency store to prevent duplicate message side-effects
builder.Services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

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

var host = builder.Build();
host.Run();
