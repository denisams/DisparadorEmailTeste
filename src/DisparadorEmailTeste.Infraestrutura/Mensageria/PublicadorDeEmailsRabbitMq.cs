using System.Text.Json;
using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DisparadorEmailTeste.Infraestrutura.Mensageria;

/// <summary>
/// Publica cada <see cref="Email"/> como uma mensagem individual, durável, na fila de envio.
/// Um único <see cref="IChannel"/> é reaproveitado entre publicações; como canais não são
/// seguros para uso concorrente, o acesso é serializado por um semáforo.
/// </summary>
public sealed class PublicadorDeEmailsRabbitMq : IPublicadorDeEmails, IAsyncDisposable
{
    private readonly ConexaoRabbitMq _conexao;
    private readonly ConfiguracaoRabbitMq _configuracao;
    private readonly SemaphoreSlim _trava = new(1, 1);
    private IChannel? _canal;

    public PublicadorDeEmailsRabbitMq(ConexaoRabbitMq conexao, IOptions<ConfiguracaoRabbitMq> opcoes)
    {
        _conexao = conexao;
        _configuracao = opcoes.Value;
    }

    public async Task PublicarAsync(Email email, CancellationToken cancelamento)
    {
        var corpo = JsonSerializer.SerializeToUtf8Bytes(email);
        var propriedades = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await _trava.WaitAsync(cancelamento);
        try
        {
            var canal = await ObterCanalAsync(cancelamento);

            await canal.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _configuracao.NomeFilaEnvio,
                mandatory: false,
                basicProperties: propriedades,
                body: corpo,
                cancellationToken: cancelamento);
        }
        finally
        {
            _trava.Release();
        }
    }

    private async Task<IChannel> ObterCanalAsync(CancellationToken cancelamento)
    {
        if (_canal is { IsOpen: true })
        {
            return _canal;
        }

        _canal = await _conexao.CriarCanalAsync(cancelamento);
        await TopologiaRabbitMq.DeclararAsync(_canal, _configuracao, cancelamento);
        return _canal;
    }

    public async ValueTask DisposeAsync()
    {
        if (_canal is not null)
        {
            await _canal.CloseAsync();
            _canal.Dispose();
        }

        _trava.Dispose();
    }
}
