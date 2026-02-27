namespace CompraProgramada.Application.DTOs;

public record AlterarValorMensalResponse(
    long ClienteId,
    decimal ValorMensalAnterior,
    decimal ValorMensalNovo,
    DateTime DataAlteracao,
    string Mensagem);
