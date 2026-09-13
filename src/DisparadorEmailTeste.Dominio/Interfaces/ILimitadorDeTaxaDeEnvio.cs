namespace DisparadorEmailTeste.Dominio.Interfaces;

/// <summary>
/// Controla a taxa de envio (emails por segundo). O consumidor da fila deve aguardar
/// uma permissão antes de disparar cada email.
/// </summary>
public interface ILimitadorDeTaxaDeEnvio
{
    Task AguardarPermissaoAsync(CancellationToken cancelamento);
}
