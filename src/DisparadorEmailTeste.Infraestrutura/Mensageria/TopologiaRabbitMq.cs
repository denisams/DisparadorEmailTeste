using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using RabbitMQ.Client;

namespace DisparadorEmailTeste.Infraestrutura.Mensageria;

/// <summary>
/// Declara a topologia de filas/exchanges usada pelo disparador. É chamada tanto pelo
/// publicador (Api) quanto pelo consumidor (Worker) — a declaração é idempotente, então
/// não importa qual dos dois processos sobe primeiro.
/// </summary>
public static class TopologiaRabbitMq
{
    public static string NomeExchangeMorta(ConfiguracaoRabbitMq configuracao) => $"{configuracao.NomeFilaEnvio}.dlx";

    public static async Task DeclararAsync(IChannel canal, ConfiguracaoRabbitMq configuracao, CancellationToken cancelamento)
    {
        var exchangeMorta = NomeExchangeMorta(configuracao);

        await canal.ExchangeDeclareAsync(
            exchange: exchangeMorta,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancelamento);

        await canal.QueueDeclareAsync(
            queue: configuracao.NomeFilaMorta,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancelamento);

        await canal.QueueBindAsync(
            queue: configuracao.NomeFilaMorta,
            exchange: exchangeMorta,
            routingKey: string.Empty,
            cancellationToken: cancelamento);

        await canal.QueueDeclareAsync(
            queue: configuracao.NomeFilaEnvio,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = exchangeMorta },
            cancellationToken: cancelamento);
    }
}
