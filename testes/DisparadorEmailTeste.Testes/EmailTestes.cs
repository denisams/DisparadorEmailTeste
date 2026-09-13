using DisparadorEmailTeste.Dominio.Entidades;

namespace DisparadorEmailTeste.Testes;

public class EmailTestes
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherTodosOsCampos()
    {
        var email = Email.Criar("destinatario@exemplo.com", "Assunto", "<p>corpo</p>", "corpo texto", "remetente@exemplo.com");

        Assert.Equal("destinatario@exemplo.com", email.Destinatario);
        Assert.Equal("Assunto", email.Assunto);
        Assert.Equal("<p>corpo</p>", email.CorpoHtml);
        Assert.Equal("corpo texto", email.CorpoTexto);
        Assert.Equal("remetente@exemplo.com", email.Remetente);
        Assert.Equal(0, email.TentativasDeEnvio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba.com")]
    public void Criar_ComDestinatarioInvalido_DeveLancarArgumentException(string destinatarioInvalido)
    {
        Assert.Throws<ArgumentException>(() => Email.Criar(destinatarioInvalido, "Assunto", "<p>corpo</p>"));
    }

    [Fact]
    public void Criar_ComAssuntoVazio_DeveLancarArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.Criar("destinatario@exemplo.com", "", "<p>corpo</p>"));
    }

    [Fact]
    public void Criar_ComCorpoVazio_DeveLancarArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.Criar("destinatario@exemplo.com", "Assunto", ""));
    }
}
