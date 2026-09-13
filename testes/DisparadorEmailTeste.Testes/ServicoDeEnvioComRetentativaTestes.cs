using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Excecoes;
using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Envio;
using Microsoft.Extensions.Logging.Abstractions;

namespace DisparadorEmailTeste.Testes;

public class ServicoDeEnvioComRetentativaTestes
{
    private sealed class ServicoFalsoDeEnvio : IServicoDeEnvioDeEmail
    {
        private readonly Queue<Exception?> _resultados;

        public int QuantidadeDeChamadas { get; private set; }

        public ServicoFalsoDeEnvio(params Exception?[] resultados)
        {
            _resultados = new Queue<Exception?>(resultados);
        }

        public Task EnviarAsync(Email email, CancellationToken cancelamento)
        {
            QuantidadeDeChamadas++;
            var erro = _resultados.Dequeue();
            return erro is null ? Task.CompletedTask : Task.FromException(erro);
        }
    }

    private static Email CriarEmailDeTeste() => Email.Criar("destinatario@exemplo.com", "Assunto", "<p>corpo</p>");

    [Fact]
    public async Task EnviarAsync_ComFalhaTransitoriaSeguidaDeSucesso_DeveTentarNovamenteEConcluir()
    {
        var servicoFalso = new ServicoFalsoDeEnvio(new TimeoutException(), null);
        var servico = new ServicoDeEnvioComRetentativa(servicoFalso, NullLogger<ServicoDeEnvioComRetentativa>.Instance);

        await servico.EnviarAsync(CriarEmailDeTeste(), CancellationToken.None);

        Assert.Equal(2, servicoFalso.QuantidadeDeChamadas);
    }

    [Fact]
    public async Task EnviarAsync_ComFalhaPermanente_NaoDeveTentarNovamente()
    {
        var servicoFalso = new ServicoFalsoDeEnvio(new FalhaPermanenteDeEnvioException("rejeitado"));
        var servico = new ServicoDeEnvioComRetentativa(servicoFalso, NullLogger<ServicoDeEnvioComRetentativa>.Instance);

        await Assert.ThrowsAsync<FalhaPermanenteDeEnvioException>(() => servico.EnviarAsync(CriarEmailDeTeste(), CancellationToken.None));

        Assert.Equal(1, servicoFalso.QuantidadeDeChamadas);
    }
}
