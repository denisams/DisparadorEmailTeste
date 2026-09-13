using DisparadorEmailTeste.Infraestrutura;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((provedor, configuracaoLog) => configuracaoLog
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(provedor)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AdicionarPublicacaoDeEmails(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "saudavel" }));

app.Run();
