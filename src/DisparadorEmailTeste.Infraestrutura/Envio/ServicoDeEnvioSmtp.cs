using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Excecoes;
using DisparadorEmailTeste.Dominio.Interfaces;
using DisparadorEmailTeste.Infraestrutura.Configuracoes;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DisparadorEmailTeste.Infraestrutura.Envio;

public sealed class ServicoDeEnvioSmtp : IServicoDeEnvioDeEmail
{
    private readonly ConfiguracaoSmtp _configuracao;

    public ServicoDeEnvioSmtp(IOptions<ConfiguracaoSmtp> opcoes)
    {
        _configuracao = opcoes.Value;
    }

    public async Task EnviarAsync(Email email, CancellationToken cancelamento)
    {
        var mensagem = MontarMensagem(email);

        using var cliente = new SmtpClient();
        cliente.Timeout = _configuracao.TimeoutEmSegundos * 1000;

        try
        {
            var opcaoSsl = _configuracao.UsarSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await cliente.ConnectAsync(_configuracao.Host, _configuracao.Porta, opcaoSsl, cancelamento);

            if (!string.IsNullOrWhiteSpace(_configuracao.Usuario))
            {
                await cliente.AuthenticateAsync(_configuracao.Usuario, _configuracao.Senha ?? string.Empty, cancelamento);
            }

            await cliente.SendAsync(mensagem, cancelamento);
        }
        catch (SmtpCommandException erro) when (EhRejeicaoPermanente(erro))
        {
            throw new FalhaPermanenteDeEnvioException($"Servidor SMTP rejeitou o email para '{email.Destinatario}': {erro.Message}", erro);
        }
        finally
        {
            if (cliente.IsConnected)
            {
                await cliente.DisconnectAsync(true, cancelamento);
            }
        }
    }

    private MimeMessage MontarMensagem(Email email)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(email.Remetente ?? _configuracao.RemetentePadrao));
        mensagem.To.Add(MailboxAddress.Parse(email.Destinatario));
        mensagem.Subject = email.Assunto;

        var corpo = new BodyBuilder { HtmlBody = email.CorpoHtml, TextBody = email.CorpoTexto };
        mensagem.Body = corpo.ToMessageBody();

        return mensagem;
    }

    /// <summary>
    /// Códigos de resposta SMTP na faixa 5xx (exceto limite de taxa/indisponibilidade transitória)
    /// indicam que repetir o envio não vai adiantar — ex.: caixa inexistente, endereço rejeitado.
    /// </summary>
    private static bool EhRejeicaoPermanente(SmtpCommandException erro) =>
        erro.StatusCode is SmtpStatusCode.MailboxUnavailable
            or SmtpStatusCode.MailboxNameNotAllowed
            or SmtpStatusCode.UserNotLocalTryAlternatePath;
}
