using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces;

public interface IMotorCompraService
{
    /// <summary>
    /// RN-020 a RN-044: Executa o ciclo completo de compra para uma data de referência.
    /// </summary>
    Task<ResultadoCompraResponse> ExecutarCompraAsync(DateTime dataReferencia);

    /// <summary>
    /// RN-020/021/022: Calcula a data efetiva de execução (ajusta fim de semana para segunda).
    /// </summary>
    DateTime CalcularDataExecucao(DateTime dataReferencia);

    /// <summary>
    /// RN-020: Retorna as 3 datas de compra do mês (dias 5, 15, 25 ajustados).
    /// </summary>
    IReadOnlyList<DateTime> ObterDatasCompraMes(int ano, int mes);
}
