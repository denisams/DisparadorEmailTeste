using DisparadorEmailTeste.Dominio.Entidades;

namespace DisparadorEmailTeste.Dominio.Interfaces;

/// <summary>
/// Publica emails na fila de processamento. Implementado pela infraestrutura de mensageria
/// (RabbitMQ), consumido pela Api ao aceitar uma solicitação de disparo.
/// </summary>
public interface IPublicadorDeEmails
{
    Task PublicarAsync(Email email, CancellationToken cancelamento);
}
