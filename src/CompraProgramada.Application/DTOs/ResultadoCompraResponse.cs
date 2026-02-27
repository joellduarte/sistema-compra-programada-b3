namespace CompraProgramada.Application.DTOs;

public record ResultadoCompraResponse(
    DateTime DataReferencia,
    DateTime DataExecucao,
    int TotalClientesAtivos,
    decimal ValorTotalConsolidado,
    List<OrdemCompraDto> Ordens,
    List<DistribuicaoResumoDto> Distribuicoes,
    string Mensagem);

public record OrdemCompraDto(
    string Ticker,
    string TickerNegociacao,
    int Quantidade,
    decimal PrecoUnitario,
    string TipoMercado,
    decimal ValorTotal);

public record DistribuicaoResumoDto(
    string Ticker,
    int TotalDistribuido,
    int ResiduoMaster,
    int TotalClientes);
