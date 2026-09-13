using DisparadorEmailTeste.Infraestrutura;
using DisparadorEmailTeste.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((provedor, configuracaoLog) => configuracaoLog
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(provedor)
    .Enrich.FromLogContext());

builder.Services.AdicionarConsumoDeEmails(builder.Configuration);
builder.Services.AddHostedService<ConsumidorDeEmailsWorker>();

var host = builder.Build();
host.Run();
