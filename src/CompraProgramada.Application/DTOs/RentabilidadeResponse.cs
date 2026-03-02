namespace CompraProgramada.Application.DTOs;

public record RentabilidadeResponse(
    long ClienteId,
    string Nome,
    DateTime DataConsulta,
    ResumoCarteiraDto Rentabilidade,
    IReadOnlyList<AporteHistoricoDto> HistoricoAportes,
    IReadOnlyList<EvolucaoCarteiraDto> EvolucaoCarteira);

public record AporteHistoricoDto(
    string Data,
    decimal Valor,
    string Parcela);

public record EvolucaoCarteiraDto(
    string Data,
    decimal ValorCarteira,
    decimal ValorInvestido,
    decimal Rentabilidade);
