namespace DisparadorEmailTeste.Infraestrutura.Configuracoes;

public sealed class ConfiguracaoSmtp
{
    public const string Secao = "Smtp";

    public required string Host { get; init; }
    public int Porta { get; init; } = 587;
    public string? Usuario { get; init; }
    public string? Senha { get; init; }
    public bool UsarSsl { get; init; } = true;
    public required string RemetentePadrao { get; init; }
    public int TimeoutEmSegundos { get; init; } = 30;
}
