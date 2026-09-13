using DisparadorEmailTeste.Dominio.Entidades;

namespace DisparadorEmailTeste.Dominio.Interfaces;

/// <summary>
/// Abstrai o mecanismo real de envio (SMTP hoje; poderia ser trocado por uma API
/// de provedor transacional sem alterar o restante do fluxo).
/// </summary>
public interface IServicoDeEnvioDeEmail
{
    Task EnviarAsync(Email email, CancellationToken cancelamento);
}
