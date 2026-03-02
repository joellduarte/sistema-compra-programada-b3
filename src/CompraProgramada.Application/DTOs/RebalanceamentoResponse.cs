namespace CompraProgramada.Application.DTOs;

public record RebalanceamentoResponse(
    string Tipo,
    int TotalClientesRebalanceados,
    List<RebalanceamentoClienteDto> Detalhes,
    string Mensagem);

public record RebalanceamentoClienteDto(
    long ClienteId,
    string Nome,
    List<OperacaoRebalanceamentoDto> Vendas,
    List<OperacaoRebalanceamentoDto> Compras,
    decimal TotalVendas,
    decimal TotalCompras,
    decimal LucroLiquido);

public record OperacaoRebalanceamentoDto(
    string Ticker,
    int Quantidade,
    decimal Preco,
    decimal ValorTotal);
