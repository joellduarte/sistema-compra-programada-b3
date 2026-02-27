namespace CompraProgramada.Application.DTOs;

public record SaidaResponse(
    long ClienteId,
    string Nome,
    bool Ativo,
    DateTime? DataSaida,
    string Mensagem);
