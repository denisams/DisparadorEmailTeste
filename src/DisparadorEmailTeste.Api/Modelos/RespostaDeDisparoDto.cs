namespace DisparadorEmailTeste.Api.Modelos;

public sealed class RespostaDeDisparoDto
{
    public required int QuantidadeEnfileirada { get; init; }
    public required List<string> DestinatariosInvalidos { get; init; }
}
