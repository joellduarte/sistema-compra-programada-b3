using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class MotorCompraService : IMotorCompraService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IOrdemCompraRepository _ordemCompraRepository;
    private readonly IDistribuicaoRepository _distribuicaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MotorCompraService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICotacaoRepository cotacaoRepository,
        ICestaRecomendacaoRepository cestaRepository,
        IOrdemCompraRepository ordemCompraRepository,
        IDistribuicaoRepository distribuicaoRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cotacaoRepository = cotacaoRepository;
        _cestaRepository = cestaRepository;
        _ordemCompraRepository = ordemCompraRepository;
        _distribuicaoRepository = distribuicaoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultadoCompraResponse> ExecutarCompraAsync(DateTime dataReferencia)
    {
        var dataExecucao = CalcularDataExecucao(dataReferencia);

        // Verificar se já executou para esta data
        if (await _ordemCompraRepository.ExisteParaDataAsync(dataReferencia))
            throw new InvalidOperationException(
                $"COMPRA_JA_EXECUTADA:Compra já foi executada para a data {dataReferencia:dd/MM/yyyy}.");

        // RN-018: Obter cesta ativa
        var cesta = await _cestaRepository.ObterAtivaAsync()
            ?? throw new InvalidOperationException(
                "CESTA_NAO_ENCONTRADA:Nenhuma cesta de recomendação ativa.");

        // RN-024: Apenas clientes ativos
        var clientesAtivos = await _clienteRepository.ObterAtivosAsync();
        if (clientesAtivos.Count == 0)
            throw new InvalidOperationException(
                "SEM_CLIENTES_ATIVOS:Nenhum cliente ativo para executar compra.");

        // Obter conta master
        var contaMaster = await _contaGraficaRepository.ObterMasterAsync()
            ?? throw new InvalidOperationException(
                "CONTA_MASTER_NAO_ENCONTRADA:Conta master não encontrada.");

        // RN-025/026: Calcular aportes individuais e total consolidado
        var aportes = new List<(Cliente Cliente, decimal Valor)>();
        foreach (var cliente in clientesAtivos)
        {
            var parcela = cliente.CalcularValorParcela(); // RN-023: 1/3 do valor mensal
            aportes.Add((cliente, parcela));
        }
        var totalConsolidado = aportes.Sum(a => a.Valor);

        // Obter cotações e calcular quantidades por ativo
        var ordensDto = new List<OrdemCompraDto>();
        var distribuicoesResumo = new List<DistribuicaoResumoDto>();
        var ordensEntidade = new List<OrdemCompra>();

        foreach (var item in cesta.Itens)
        {
            // RN-026: Valor destinado ao ativo
            var valorAtivo = totalConsolidado * (item.Percentual / 100m);

            // RN-027: Cotação de fechamento do último pregão
            var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(item.Ticker);
            if (cotacao is null)
                continue; // Sem cotação, pula o ativo

            var precoCotacao = cotacao.PrecoFechamento;

            // RN-028: Quantidade = TRUNCAR(Valor / Cotação)
            var quantidadeCalculada = (int)(valorAtivo / precoCotacao);
            if (quantidadeCalculada <= 0)
                continue;

            // RN-029/030: Verificar saldo na custódia master e descontar
            var custodiaMaster = await _custodiaRepository
                .ObterPorContaETickerAsync(contaMaster.Id, item.Ticker);
            var saldoMaster = custodiaMaster?.Quantidade ?? 0;

            // Total disponível = calculado (o que comprar + saldo master)
            var totalDisponivel = quantidadeCalculada + saldoMaster;

            // Quantidade efetiva a comprar (descontando saldo)
            var quantidadeAComprar = Math.Max(0, quantidadeCalculada - saldoMaster);

            if (quantidadeAComprar > 0)
            {
                // RN-031/032/033: Separar lote padrão e fracionário
                var (lote, fracionario) = OrdemCompra.SepararLoteEFracionario(quantidadeAComprar);

                if (lote > 0)
                {
                    var ordemLote = OrdemCompra.Criar(
                        contaMaster.Id, item.Ticker, lote, precoCotacao,
                        TipoMercado.LotePadrao, dataReferencia);
                    await _ordemCompraRepository.AdicionarAsync(ordemLote);
                    ordensEntidade.Add(ordemLote);

                    ordensDto.Add(new OrdemCompraDto(
                        item.Ticker, ordemLote.ObterTickerNegociacao(),
                        lote, precoCotacao, "LOTE_PADRAO", ordemLote.CalcularValorTotal()));
                }

                if (fracionario > 0)
                {
                    var ordemFrac = OrdemCompra.Criar(
                        contaMaster.Id, item.Ticker, fracionario, precoCotacao,
                        TipoMercado.Fracionario, dataReferencia);
                    await _ordemCompraRepository.AdicionarAsync(ordemFrac);
                    ordensEntidade.Add(ordemFrac);

                    ordensDto.Add(new OrdemCompraDto(
                        item.Ticker, ordemFrac.ObterTickerNegociacao(),
                        fracionario, precoCotacao, "FRACIONARIO", ordemFrac.CalcularValorTotal()));
                }

                // Atualizar custódia master com as ações compradas
                if (custodiaMaster is null)
                {
                    custodiaMaster = Custodia.Criar(contaMaster.Id, item.Ticker);
                    await _custodiaRepository.AdicionarAsync(custodiaMaster);
                }
                custodiaMaster.AdicionarAcoes(quantidadeAComprar, precoCotacao);
                await _custodiaRepository.AtualizarAsync(custodiaMaster);
            }

            // RN-034 a RN-040: Distribuição proporcional
            var totalDistribuido = 0;
            var clientesDistribuidos = 0;

            // totalDisponivel = ações compradas agora + saldo master anterior
            // Agora a custódia master tem todas as ações disponíveis
            var qtdParaDistribuir = quantidadeAComprar > 0
                ? quantidadeAComprar + saldoMaster
                : saldoMaster;

            if (qtdParaDistribuir <= 0)
            {
                distribuicoesResumo.Add(new DistribuicaoResumoDto(
                    item.Ticker, 0, custodiaMaster?.Quantidade ?? 0, 0));
                continue;
            }

            foreach (var (cliente, aporte) in aportes)
            {
                // RN-035: Proporção do cliente
                var proporcao = aporte / totalConsolidado;

                // RN-036: Quantidade = TRUNCAR(Proporção × Quantidade Total Disponível)
                var qtdCliente = (int)(proporcao * qtdParaDistribuir);
                if (qtdCliente <= 0)
                    continue;

                // Obter conta gráfica filhote do cliente
                var contaFilhote = await _contaGraficaRepository.ObterPorClienteIdAsync(cliente.Id);
                if (contaFilhote is null)
                    continue;

                // Obter ou criar custódia filhote
                var custodiaFilhote = await _custodiaRepository
                    .ObterPorContaETickerAsync(contaFilhote.Id, item.Ticker);
                if (custodiaFilhote is null)
                {
                    custodiaFilhote = Custodia.Criar(contaFilhote.Id, item.Ticker);
                    await _custodiaRepository.AdicionarAsync(custodiaFilhote);
                }

                // RN-038/041/042/044: Atualizar preço médio da custódia filhote
                custodiaFilhote.AdicionarAcoes(qtdCliente, precoCotacao);
                await _custodiaRepository.AtualizarAsync(custodiaFilhote);

                // Registrar distribuição (vincula à primeira ordem deste ticker)
                var ordemRef = ordensEntidade.FirstOrDefault(o => o.Ticker == item.Ticker);
                if (ordemRef is not null)
                {
                    var dist = Distribuicao.Criar(
                        ordemRef.Id, custodiaFilhote.Id,
                        item.Ticker, qtdCliente, precoCotacao);
                    await _distribuicaoRepository.AdicionarAsync(dist);
                }

                // Remover da custódia master
                custodiaMaster!.RemoverAcoes(qtdCliente, precoCotacao);
                await _custodiaRepository.AtualizarAsync(custodiaMaster);

                totalDistribuido += qtdCliente;
                clientesDistribuidos++;
            }

            // RN-039: Resíduo permanece na custódia master
            var residuo = custodiaMaster?.Quantidade ?? 0;

            distribuicoesResumo.Add(new DistribuicaoResumoDto(
                item.Ticker, totalDistribuido, residuo, clientesDistribuidos));
        }

        await _unitOfWork.CommitAsync();

        return new ResultadoCompraResponse(
            dataReferencia,
            dataExecucao,
            clientesAtivos.Count,
            totalConsolidado,
            ordensDto,
            distribuicoesResumo,
            $"Compra executada com sucesso para {dataReferencia:dd/MM/yyyy}. " +
            $"{clientesAtivos.Count} clientes, {ordensDto.Count} ordens geradas.");
    }

    /// <summary>
    /// RN-020/021/022: Se cair em sábado ou domingo, ajusta para segunda-feira.
    /// </summary>
    public DateTime CalcularDataExecucao(DateTime dataReferencia)
    {
        return dataReferencia.DayOfWeek switch
        {
            DayOfWeek.Saturday => dataReferencia.AddDays(2),
            DayOfWeek.Sunday => dataReferencia.AddDays(1),
            _ => dataReferencia
        };
    }

    /// <summary>
    /// RN-020: Retorna as 3 datas de compra do mês (dias 5, 15, 25) ajustadas.
    /// </summary>
    public IReadOnlyList<DateTime> ObterDatasCompraMes(int ano, int mes)
    {
        var datas = new List<DateTime>();
        foreach (var dia in new[] { 5, 15, 25 })
        {
            var data = new DateTime(ano, mes, dia);
            datas.Add(CalcularDataExecucao(data));
        }
        return datas;
    }
}
