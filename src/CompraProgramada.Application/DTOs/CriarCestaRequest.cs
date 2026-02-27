namespace CompraProgramada.Application.DTOs;

public record CriarCestaRequest(
    string Nome,
    List<ItemCestaRequest> Itens);

public record ItemCestaRequest(
    string Ticker,
    decimal Percentual);
