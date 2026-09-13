namespace DisparadorEmailTeste.Api.Modelos;

public sealed class SolicitacaoDeDisparoDto
{
    public required string Assunto { get; init; }
    public required string CorpoHtml { get; init; }
    public string? CorpoTexto { get; init; }
    public string? Remetente { get; init; }
    public required List<string> Destinatarios { get; init; }
}
