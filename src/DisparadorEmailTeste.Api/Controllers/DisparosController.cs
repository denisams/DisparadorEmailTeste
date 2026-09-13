using DisparadorEmailTeste.Api.Modelos;
using DisparadorEmailTeste.Dominio.Entidades;
using DisparadorEmailTeste.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DisparadorEmailTeste.Api.Controllers;

[ApiController]
[Route("api/disparos")]
public sealed class DisparosController : ControllerBase
{
    private readonly IPublicadorDeEmails _publicador;
    private readonly ILogger<DisparosController> _logger;

    public DisparosController(IPublicadorDeEmails publicador, ILogger<DisparosController> logger)
    {
        _publicador = publicador;
        _logger = logger;
    }

    /// <summary>
    /// Aceita um disparo em lote e o publica na fila, um email por destinatário.
    /// O envio real é assíncrono: esta chamada retorna assim que os itens são enfileirados,
    /// não quando os emails são efetivamente entregues.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RespostaDeDisparoDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespostaDeDisparoDto>> DispararAsync(
        [FromBody] SolicitacaoDeDisparoDto solicitacao,
        CancellationToken cancelamento)
    {
        if (solicitacao.Destinatarios.Count == 0)
        {
            return BadRequest("Informe ao menos um destinatário.");
        }

        var destinatariosInvalidos = new List<string>();
        var quantidadeEnfileirada = 0;

        foreach (var destinatario in solicitacao.Destinatarios)
        {
            Email email;
            try
            {
                email = Email.Criar(destinatario, solicitacao.Assunto, solicitacao.CorpoHtml, solicitacao.CorpoTexto, solicitacao.Remetente);
            }
            catch (ArgumentException)
            {
                destinatariosInvalidos.Add(destinatario);
                continue;
            }

            await _publicador.PublicarAsync(email, cancelamento);
            quantidadeEnfileirada++;
        }

        _logger.LogInformation(
            "Disparo aceito: {QuantidadeEnfileirada} email(s) enfileirado(s), {QuantidadeInvalida} destinatário(s) inválido(s).",
            quantidadeEnfileirada,
            destinatariosInvalidos.Count);

        return Accepted(new RespostaDeDisparoDto
        {
            QuantidadeEnfileirada = quantidadeEnfileirada,
            DestinatariosInvalidos = destinatariosInvalidos,
        });
    }
}
