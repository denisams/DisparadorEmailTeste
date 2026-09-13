namespace DisparadorEmailTeste.Dominio.Entidades;

/// <summary>
/// Representa um email individual a ser enviado. É a unidade de trabalho enfileirada
/// entre a Api (produtora) e o Worker (consumidor).
/// </summary>
public sealed class Email
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Destinatario { get; init; }
    public required string Assunto { get; init; }
    public required string CorpoHtml { get; init; }
    public string? CorpoTexto { get; init; }
    public string? Remetente { get; init; }
    public DateTimeOffset CriadoEm { get; init; } = DateTimeOffset.UtcNow;
    public int TentativasDeEnvio { get; set; }

    public static Email Criar(string destinatario, string assunto, string corpoHtml, string? corpoTexto = null, string? remetente = null)
    {
        if (string.IsNullOrWhiteSpace(destinatario) || !destinatario.Contains('@'))
        {
            throw new ArgumentException($"Destinatário inválido: '{destinatario}'.", nameof(destinatario));
        }

        if (string.IsNullOrWhiteSpace(assunto))
        {
            throw new ArgumentException("Assunto não pode ser vazio.", nameof(assunto));
        }

        if (string.IsNullOrWhiteSpace(corpoHtml))
        {
            throw new ArgumentException("Corpo do email não pode ser vazio.", nameof(corpoHtml));
        }

        return new Email
        {
            Destinatario = destinatario.Trim(),
            Assunto = assunto.Trim(),
            CorpoHtml = corpoHtml,
            CorpoTexto = corpoTexto,
            Remetente = remetente,
        };
    }
}
