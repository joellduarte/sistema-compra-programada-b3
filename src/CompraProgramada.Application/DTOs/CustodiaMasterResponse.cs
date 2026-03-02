namespace CompraProgramada.Application.DTOs;

public record CustodiaMasterResponse(
    ContaMasterDto ContaMaster,
    IReadOnlyList<AtivoMasterDto> Custodia,
    decimal ValorTotalResiduo);

public record ContaMasterDto(
    long Id,
    string NumeroConta,
    string Tipo);

public record AtivoMasterDto(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal ValorAtual);
