using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Excecoes;
using DisparadorEmailTeste.Dominio.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace DisparadorEmailTeste.Infraestrutura.Envio;

/// <summary>
/// Decora o envio real com novas tentativas (backoff exponencial com jitter) para falhas
/// transitórias — timeout de rede, servidor SMTP temporariamente indisponível, etc.
/// <see cref="FalhaPermanenteDeEnvioException"/> nunca é reprocessada aqui: propaga direto
/// para que o consumidor da fila mande a mensagem para a fila morta sem desperdiçar tentativas.
/// </summary>
public sealed class ServicoDeEnvioComRetentativa : IServicoDeEnvioDeEmail
{
    private const int NumeroMaximoDeTentativas = 3;

    private readonly IServicoDeEnvioDeEmail _servicoInterno;
    private readonly ResiliencePipeline _pipeline;

    public ServicoDeEnvioComRetentativa(IServicoDeEnvioDeEmail servicoInterno, ILogger<ServicoDeEnvioComRetentativa> logger)
    {
        _servicoInterno = servicoInterno;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(erro => erro is not FalhaPermanenteDeEnvioException),
                MaxRetryAttempts = NumeroMaximoDeTentativas,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                OnRetry = argumentos =>
                {
                    logger.LogWarning(
                        argumentos.Outcome.Exception,
                        "Tentativa {NumeroDaTentativa} de envio falhou, tentando novamente em {Atraso}.",
                        argumentos.AttemptNumber + 1,
                        argumentos.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    public async Task EnviarAsync(Email email, CancellationToken cancelamento)
    {
        await _pipeline.ExecuteAsync(
            async ct =>
            {
                email.TentativasDeEnvio++;
                await _servicoInterno.EnviarAsync(email, ct);
            },
            cancelamento);
    }
}
