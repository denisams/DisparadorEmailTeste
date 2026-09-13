namespace DisparadorEmailTeste.Infraestrutura.Configuracoes;

/// <summary>
/// Controla a volumetria de envio. É o botão principal para ajustar throughput
/// sem alterar código: aumentar/diminuir conforme os limites do provedor de SMTP.
/// </summary>
public sealed class ConfiguracaoLimiteDeEnvio
{
    public const string Secao = "LimiteDeEnvio";

    /// <summary>Quantidade máxima de emails enviados por segundo (token bucket).</summary>
    public int EmailsPorSegundo { get; init; } = 5;

    /// <summary>Tamanho da fila de espera interna para permissões de envio antes de aplicar backpressure.</summary>
    public int TamanhoDaFilaDeEspera { get; init; } = 1000;
}
