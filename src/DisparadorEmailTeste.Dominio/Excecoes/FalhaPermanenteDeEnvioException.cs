namespace DisparadorEmailTeste.Dominio.Excecoes;

/// <summary>
/// Indica que o envio falhou por um motivo que não se resolve com nova tentativa
/// (ex.: endereço inexistente rejeitado pelo servidor). A mensagem deve ir para a fila morta
/// sem reprocessamento.
/// </summary>
public sealed class FalhaPermanenteDeEnvioException : Exception
{
    public FalhaPermanenteDeEnvioException(string mensagem, Exception? erroOriginal = null)
        : base(mensagem, erroOriginal)
    {
    }
}
