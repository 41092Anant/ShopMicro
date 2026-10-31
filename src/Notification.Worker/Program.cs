using MassTransit;
using Notification.Worker.Consumers;
using Notification.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog structured logging with Console and Seq sinks
builder.Services.AddSerilog((services, lc) =>
{
    lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqUrl = builder.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        lc.WriteTo.Seq(seqUrl);
    }
});

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
