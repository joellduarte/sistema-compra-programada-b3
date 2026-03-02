using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IHistoricoValorMensalRepository _historicoRepository;
    private readonly IDistribuicaoRepository _distribuicaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICotacaoRepository cotacaoRepository,
        IHistoricoValorMensalRepository historicoRepository,
        IDistribuicaoRepository distribuicaoRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cotacaoRepository = cotacaoRepository;
        _historicoRepository = historicoRepository;
        _distribuicaoRepository = distribuicaoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
    {
        // RN-002: CPF único
        var clienteExistente = await _clienteRepository.ObterPorCpfAsync(request.Cpf);
        if (clienteExistente is not null)
            throw new InvalidOperationException("CLIENTE_CPF_DUPLICADO:CPF já cadastrado no sistema.");

        // RN-001/RN-003: Cria cliente com validações de domínio
        var cliente = Cliente.Criar(request.Nome, request.Cpf, request.Email, request.ValorMensal);
        await _clienteRepository.AdicionarAsync(cliente);
        await _unitOfWork.CommitAsync();

        // RN-004: Cria conta gráfica filhote
        var proximoNumero = await _contaGraficaRepository.ObterProximoNumeroContaAsync();
        var numeroConta = $"FLH-{proximoNumero:D6}";
        var contaGrafica = ContaGrafica.CriarFilhote(cliente.Id, numeroConta);
        await _contaGraficaRepository.AdicionarAsync(contaGrafica);

        cliente.VincularContaGrafica(contaGrafica);
        await _unitOfWork.CommitAsync();

        return new AdesaoResponse(
            cliente.Id,
            cliente.Nome,
            cliente.CPF.Numero,
            cliente.Email,
            cliente.ValorMensal,
            cliente.Ativo,
            cliente.DataAdesao,
            new ContaGraficaDto(
                contaGrafica.Id,
                contaGrafica.NumeroConta,
                contaGrafica.Tipo.ToString().ToUpperInvariant(),
                contaGrafica.DataCriacao));
    }

    public async Task<SaidaResponse> SairAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO:Cliente não encontrado.");

        // RN-007 a RN-009: Sair do produto
        cliente.Sair();
        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();

        return new SaidaResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Ativo,
            cliente.DataSaida,
            "Adesão encerrada. Sua posição em custódia foi mantida.");
    }

    public async Task<AlterarValorMensalResponse> AlterarValorMensalAsync(
        long clienteId, AlterarValorMensalRequest request)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO:Cliente não encontrado.");

        if (!cliente.Ativo)
            throw new InvalidOperationException("CLIENTE_JA_INATIVO:Cliente já está inativo.");

        // RN-011 a RN-013
        var valorAnterior = cliente.AlterarValorMensal(request.NovoValorMensal);

        // RN-013: Registrar histórico
        var historico = HistoricoValorMensal.Criar(cliente.Id, valorAnterior, request.NovoValorMensal);
        await _historicoRepository.AdicionarAsync(historico);

        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();

        return new AlterarValorMensalResponse(
            cliente.Id,
            valorAnterior,
            request.NovoValorMensal,
            historico.DataAlteracao,
            "Valor mensal atualizado. O novo valor será considerado a partir da próxima data de compra.");
    }

    public async Task<CarteiraResponse> ConsultarCarteiraAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO:Cliente não encontrado.");

        var contaGrafica = await _contaGraficaRepository.ObterPorClienteIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO:Conta gráfica não encontrada.");

        var custodias = await _custodiaRepository.ObterPorContaGraficaIdAsync(contaGrafica.Id);

        var ativos = new List<AtivoCarteiraDto>();
        decimal valorTotalInvestido = 0;
        decimal valorAtualCarteira = 0;

        foreach (var custodia in custodias)
        {
            if (custodia.Quantidade == 0)
                continue;

            var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(custodia.Ticker);
            var cotacaoAtual = cotacao?.PrecoFechamento ?? custodia.PrecoMedio;

            var valorInvestido = custodia.Quantidade * custodia.PrecoMedio;
            var valorAtual = custodia.CalcularValorAtual(cotacaoAtual);
            var pl = custodia.CalcularPL(cotacaoAtual);
            var plPercentual = valorInvestido > 0
                ? Math.Round(pl / valorInvestido * 100, 2)
                : 0m;

            valorTotalInvestido += valorInvestido;
            valorAtualCarteira += valorAtual;

            ativos.Add(new AtivoCarteiraDto(
                custodia.Ticker,
                custodia.Quantidade,
                Math.Round(custodia.PrecoMedio, 2),
                Math.Round(cotacaoAtual, 2),
                Math.Round(valorAtual, 2),
                Math.Round(pl, 2),
                plPercentual,
                0)); // composição será calculada abaixo
        }

        // Calcular composição da carteira (% de cada ativo no total)
        var ativosComComposicao = ativos.Select(a => a with
        {
            ComposicaoCarteira = valorAtualCarteira > 0
                ? Math.Round(a.ValorAtual / valorAtualCarteira * 100, 2)
                : 0m
        }).ToList();

        var plTotal = valorAtualCarteira - valorTotalInvestido;
        var rentabilidade = valorTotalInvestido > 0
            ? Math.Round(plTotal / valorTotalInvestido * 100, 2)
            : 0m;

        return new CarteiraResponse(
            cliente.Id,
            cliente.Nome,
            contaGrafica.NumeroConta,
            DateTime.UtcNow,
            new ResumoCarteiraDto(
                Math.Round(valorTotalInvestido, 2),
                Math.Round(valorAtualCarteira, 2),
                Math.Round(plTotal, 2),
                rentabilidade),
            ativosComComposicao);
    }

    public async Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO:Cliente não encontrado.");

        // Obter carteira atual (reutiliza lógica existente)
        var carteira = await ConsultarCarteiraAsync(clienteId);

        // Obter distribuições para montar histórico de aportes
        var distribuicoes = await _distribuicaoRepository.ObterPorClienteAsync(clienteId);

        // Agrupar distribuições por data de execução (cada data = 1 parcela)
        var aportesPorData = distribuicoes
            .GroupBy(d => d.OrdemCompra.DataReferencia.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var historicoAportes = new List<AporteHistoricoDto>();
        var evolucaoCarteira = new List<EvolucaoCarteiraDto>();
        decimal acumuladoInvestido = 0;
        int parcelaNoMes = 0;
        int mesAnterior = -1;

        foreach (var grupo in aportesPorData)
        {
            var valorAporte = grupo.Sum(d => d.Quantidade * d.PrecoUnitario);
            acumuladoInvestido += valorAporte;

            // Controlar parcela (1/3, 2/3, 3/3) por mês
            if (grupo.Key.Month != mesAnterior)
            {
                parcelaNoMes = 1;
                mesAnterior = grupo.Key.Month;
            }
            else
            {
                parcelaNoMes++;
            }

            historicoAportes.Add(new AporteHistoricoDto(
                grupo.Key.ToString("yyyy-MM-dd"),
                Math.Round(valorAporte, 2),
                $"{parcelaNoMes}/3"));

            // Evolução: valor investido acumulado até essa data
            // (para simplificação, usamos o valor atual da carteira só no último ponto)
            evolucaoCarteira.Add(new EvolucaoCarteiraDto(
                grupo.Key.ToString("yyyy-MM-dd"),
                Math.Round(acumuladoInvestido, 2), // valor carteira na época ≈ investido (simplificação)
                Math.Round(acumuladoInvestido, 2),
                0));
        }

        // Último ponto da evolução: carteira atual real
        if (evolucaoCarteira.Count > 0)
        {
            var ultimo = evolucaoCarteira[^1];
            evolucaoCarteira[^1] = ultimo with
            {
                ValorCarteira = carteira.Resumo.ValorAtualCarteira,
                Rentabilidade = carteira.Resumo.RentabilidadePercentual
            };
        }

        return new RentabilidadeResponse(
            cliente.Id,
            cliente.Nome,
            DateTime.UtcNow,
            carteira.Resumo,
            historicoAportes,
            evolucaoCarteira);
    }
}
