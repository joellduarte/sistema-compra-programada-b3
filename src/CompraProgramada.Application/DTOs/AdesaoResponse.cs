namespace CompraProgramada.Application.DTOs;

public record AdesaoResponse(
    long ClienteId,
    string Nome,
    string Cpf,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    ContaGraficaDto ContaGrafica);

public record ContaGraficaDto(
    long Id,
    string NumeroConta,
    string Tipo,
    DateTime DataCriacao);
