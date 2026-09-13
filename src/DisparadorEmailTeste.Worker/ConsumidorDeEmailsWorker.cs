using System.Text.Json;
using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Excecoes;
using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using DisparadorEmailTeste.Infraestrutura.Mensageria;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DisparadorEmailTeste.Worker;

/// <summary>
/// Consome a fila de envio, respeitando o limite configurado de emails por segundo antes de
/// disparar cada mensagem. Falhas permanentes ou tentativas esgotadas vão para a fila morta
/// (dead-letter) em vez de serem descartadas silenciosamente.
/// </summary>
public sealed class ConsumidorDeEmailsWorker : BackgroundService
{
    private readonly ConexaoRabbitMq _conexao;
    private readonly ConfiguracaoRabbitMq _configuracaoRabbitMq;
    private readonly IServicoDeEnvioDeEmail _servicoDeEnvio;
    private readonly ILimitadorDeTaxaDeEnvio _limitador;
    private readonly ILogger<ConsumidorDeEmailsWorker> _logger;

    public ConsumidorDeEmailsWorker(
        ConexaoRabbitMq conexao,
        IOptions<ConfiguracaoRabbitMq> opcoesRabbitMq,
        IServicoDeEnvioDeEmail servicoDeEnvio,
        ILimitadorDeTaxaDeEnvio limitador,
        ILogger<ConsumidorDeEmailsWorker> logger)
    {
        _conexao = conexao;
        _configuracaoRabbitMq = opcoesRabbitMq.Value;
        _servicoDeEnvio = servicoDeEnvio;
        _limitador = limitador;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        var canal = await _conexao.CriarCanalAsync(parada);
        await TopologiaRabbitMq.DeclararAsync(canal, _configuracaoRabbitMq, parada);
        await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: _configuracaoRabbitMq.PrefetchCount, global: false, parada);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += (_, entrega) => ProcessarMensagemAsync(canal, entrega, parada);

        await canal.BasicConsumeAsync(_configuracaoRabbitMq.NomeFilaEnvio, autoAck: false, consumidor, parada);

        _logger.LogInformation(
            "Worker pronto, consumindo a fila '{Fila}'.",
            _configuracaoRabbitMq.NomeFilaEnvio);

        try
        {
            await Task.Delay(Timeout.Infinite, parada);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessarMensagemAsync(IChannel canal, BasicDeliverEventArgs entrega, CancellationToken parada)
    {
        Email? email = null;

        try
        {
            email = JsonSerializer.Deserialize<Email>(entrega.Body.Span);
            if (email is null)
            {
                throw new FalhaPermanenteDeEnvioException("Mensagem da fila não pôde ser desserializada em um Email válido.");
            }

            await _limitador.AguardarPermissaoAsync(parada);
            await _servicoDeEnvio.EnviarAsync(email, parada);

            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, parada);
            _logger.LogInformation("Email para '{Destinatario}' enviado com sucesso.", email.Destinatario);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            _logger.LogError(
                erro,
                "Falha ao enviar email para '{Destinatario}' após {Tentativas} tentativa(s); enviando para a fila morta.",
                email?.Destinatario ?? "(desconhecido)",
                email?.TentativasDeEnvio ?? 0);

            await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: false, parada);
        }
    }
}
