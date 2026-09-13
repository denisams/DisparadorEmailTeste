using System.Threading.RateLimiting;
using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using Microsoft.Extensions.Options;

namespace DisparadorEmailTeste.Infraestrutura.Limitador;

/// <summary>
/// Implementação baseada em token bucket: repõe <see cref="ConfiguracaoLimiteDeEnvio.EmailsPorSegundo"/>
/// tokens a cada segundo. Chamadores aguardam de forma assíncrona até haver um token disponível,
/// o que naturalmente aplica backpressure sobre o consumidor da fila sem descartar mensagens.
/// </summary>
public sealed class LimitadorDeTaxaDeEnvio : ILimitadorDeTaxaDeEnvio, IDisposable
{
    private readonly TokenBucketRateLimiter _limitador;

    public LimitadorDeTaxaDeEnvio(IOptions<ConfiguracaoLimiteDeEnvio> opcoes)
    {
        var configuracao = opcoes.Value;

        _limitador = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = configuracao.EmailsPorSegundo,
            TokensPerPeriod = configuracao.EmailsPorSegundo,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = configuracao.TamanhoDaFilaDeEspera,
            AutoReplenishment = true,
        });
    }

    public async Task AguardarPermissaoAsync(CancellationToken cancelamento)
    {
        using var permissao = await _limitador.AcquireAsync(1, cancelamento);

        if (!permissao.IsAcquired)
        {
            throw new InvalidOperationException("Não foi possível obter permissão do limitador de taxa de envio (fila de espera cheia).");
        }
    }

    public void Dispose() => _limitador.Dispose();
}
