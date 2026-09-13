using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using DisparadorEmailTeste.Infraestrutura.Envio;
using DisparadorEmailTeste.Infraestrutura.Limitador;
using DisparadorEmailTeste.Infraestrutura.Mensageria;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DisparadorEmailTeste.Infraestrutura;

public static class InjecaoDeDependencia
{
    /// <summary>Registra o necessário para publicar emails na fila. Usado pela Api.</summary>
    public static IServiceCollection AdicionarPublicacaoDeEmails(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddOptions<ConfiguracaoRabbitMq>()
            .Bind(configuracao.GetSection(ConfiguracaoRabbitMq.Secao))
            .ValidateOnStart();

        servicos.AddSingleton<ConexaoRabbitMq>();
        servicos.AddSingleton<IPublicadorDeEmails, PublicadorDeEmailsRabbitMq>();

        return servicos;
    }

    /// <summary>Registra o necessário para consumir a fila e enviar os emails de fato. Usado pelo Worker.</summary>
    public static IServiceCollection AdicionarConsumoDeEmails(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddOptions<ConfiguracaoRabbitMq>()
            .Bind(configuracao.GetSection(ConfiguracaoRabbitMq.Secao))
            .ValidateOnStart();

        servicos.AddOptions<ConfiguracaoSmtp>()
            .Bind(configuracao.GetSection(ConfiguracaoSmtp.Secao))
            .ValidateOnStart();

        servicos.AddOptions<ConfiguracaoLimiteDeEnvio>()
            .Bind(configuracao.GetSection(ConfiguracaoLimiteDeEnvio.Secao))
            .ValidateOnStart();

        servicos.AddSingleton<ConexaoRabbitMq>();
        servicos.AddSingleton<ILimitadorDeTaxaDeEnvio, LimitadorDeTaxaDeEnvio>();

        servicos.AddTransient<ServicoDeEnvioSmtp>();
        servicos.AddTransient<IServicoDeEnvioDeEmail>(provedor => new ServicoDeEnvioComRetentativa(
            provedor.GetRequiredService<ServicoDeEnvioSmtp>(),
            provedor.GetRequiredService<ILogger<ServicoDeEnvioComRetentativa>>()));

        return servicos;
    }
}
