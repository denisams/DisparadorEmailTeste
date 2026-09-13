using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace DisparadorEmailTeste.Infraestrutura.Mensageria;

/// <summary>
/// Mantém uma única <see cref="IConnection"/> compartilhada (conexões RabbitMQ são thread-safe
/// e caras de abrir; canais devem ser criados um por consumidor/publicador).
/// </summary>
public sealed class ConexaoRabbitMq : IAsyncDisposable
{
    private readonly ConfiguracaoRabbitMq _configuracao;
    private readonly ResiliencePipeline _politicaDeReconexao;
    private readonly SemaphoreSlim _trava = new(1, 1);
    private IConnection? _conexao;

    public ConexaoRabbitMq(IOptions<ConfiguracaoRabbitMq> opcoes, ILogger<ConexaoRabbitMq> logger)
    {
        _configuracao = opcoes.Value;

        // Em orquestradores de container é comum o processo subir antes do broker aceitar
        // conexões AMQP (mesmo já reportando saudável no health check do Erlang) — tentar
        // novamente por ~30s cobre essa corrida de inicialização sem derrubar o host.
        _politicaDeReconexao = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 6,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                OnRetry = argumentos =>
                {
                    logger.LogWarning(
                        argumentos.Outcome.Exception,
                        "Falha ao conectar no RabbitMQ ({HostName}:{Porta}), tentando novamente em {Atraso}.",
                        _configuracao.HostName,
                        _configuracao.Porta,
                        argumentos.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    public async Task<IConnection> ObterConexaoAsync(CancellationToken cancelamento)
    {
        if (_conexao is { IsOpen: true })
        {
            return _conexao;
        }

        await _trava.WaitAsync(cancelamento);
        try
        {
            if (_conexao is { IsOpen: true })
            {
                return _conexao;
            }

            var fabrica = new ConnectionFactory
            {
                HostName = _configuracao.HostName,
                Port = _configuracao.Porta,
                UserName = _configuracao.Usuario,
                Password = _configuracao.Senha,
            };

            _conexao = await _politicaDeReconexao.ExecuteAsync(
                async ct => await fabrica.CreateConnectionAsync(ct),
                cancelamento);
            return _conexao;
        }
        finally
        {
            _trava.Release();
        }
    }

    public async Task<IChannel> CriarCanalAsync(CancellationToken cancelamento)
    {
        var conexao = await ObterConexaoAsync(cancelamento);
        return await conexao.CreateChannelAsync(cancellationToken: cancelamento);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null)
        {
            await _conexao.CloseAsync();
            _conexao.Dispose();
        }

        _trava.Dispose();
    }
}
