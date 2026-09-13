using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using DisparadorEmailTeste.Infraestrutura.Limitador;
using Microsoft.Extensions.Options;

namespace DisparadorEmailTeste.Testes;

public class LimitadorDeTaxaDeEnvioTestes
{
    [Fact]
    public async Task AguardarPermissaoAsync_AbaixoDoLimite_NaoDeveBloquear()
    {
        var opcoes = Options.Create(new ConfiguracaoLimiteDeEnvio { EmailsPorSegundo = 10, TamanhoDaFilaDeEspera = 100 });
        using var limitador = new LimitadorDeTaxaDeEnvio(opcoes);

        var inicio = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await limitador.AguardarPermissaoAsync(CancellationToken.None);
        }
        var duracao = DateTime.UtcNow - inicio;

        Assert.True(duracao < TimeSpan.FromMilliseconds(500), $"Esperava liberar rápido dentro do limite, levou {duracao}.");
    }

    [Fact]
    public async Task AguardarPermissaoAsync_AcimaDoLimite_DeveAplicarBackpressure()
    {
        var opcoes = Options.Create(new ConfiguracaoLimiteDeEnvio { EmailsPorSegundo = 2, TamanhoDaFilaDeEspera = 100 });
        using var limitador = new LimitadorDeTaxaDeEnvio(opcoes);

        var inicio = DateTime.UtcNow;
        for (var i = 0; i < 4; i++)
        {
            await limitador.AguardarPermissaoAsync(CancellationToken.None);
        }
        var duracao = DateTime.UtcNow - inicio;

        Assert.True(duracao >= TimeSpan.FromMilliseconds(900), $"Esperava esperar ao menos ~1s para a 2ª leva de tokens, levou {duracao}.");
    }
}
