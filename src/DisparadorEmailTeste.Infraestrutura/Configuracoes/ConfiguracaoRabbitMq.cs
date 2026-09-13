namespace DisparadorEmailTeste.Infraestrutura.Configuracoes;

public sealed class ConfiguracaoRabbitMq
{
    public const string Secao = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Porta { get; init; } = 5672;
    public string Usuario { get; init; } = "guest";
    public string Senha { get; init; } = "guest";
    public string NomeFilaEnvio { get; init; } = "envio-de-emails";
    public string NomeFilaMorta { get; init; } = "envio-de-emails.morta";

    /// <summary>Quantas mensagens o consumidor pode ter em processamento simultâneo (prefetch).</summary>
    public ushort PrefetchCount { get; init; } = 20;
}
