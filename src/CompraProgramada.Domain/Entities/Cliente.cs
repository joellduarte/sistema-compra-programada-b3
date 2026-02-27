using CompraProgramada.Domain.ValueObjects;

namespace CompraProgramada.Domain.Entities;

public class Cliente : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public CPF CPF { get; private set; } = null!;
    public string Email { get; private set; } = string.Empty;
    public decimal ValorMensal { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime DataAdesao { get; private set; }
    public DateTime? DataSaida { get; private set; }

    // Navegação
    public ContaGrafica ContaGrafica { get; private set; } = null!;

    private Cliente() { } // EF Core

    public static Cliente Criar(string nome, string cpf, string email, decimal valorMensal)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.");

        // RN-003: valor mínimo de R$ 100,00
        if (valorMensal < 100m)
            throw new ArgumentException("O valor mensal mínimo é de R$ 100,00.");

        var cliente = new Cliente
        {
            Nome = nome.Trim(),
            CPF = CPF.Criar(cpf),    // RN-001: valida CPF
            Email = email.Trim(),
            ValorMensal = valorMensal,
            Ativo = true,             // RN-005
            DataAdesao = DateTime.UtcNow // RN-006
        };

        return cliente;
    }

    /// <summary>
    /// RN-004: Vincula a conta gráfica filhote ao cliente.
    /// </summary>
    public void VincularContaGrafica(ContaGrafica contaGrafica)
    {
        ContaGrafica = contaGrafica ?? throw new ArgumentNullException(nameof(contaGrafica));
    }

    /// <summary>
    /// RN-007 a RN-009: Cliente sai do produto, mantém posição.
    /// </summary>
    public void Sair()
    {
        if (!Ativo)
            throw new InvalidOperationException("Cliente já está inativo.");

        Ativo = false;
        DataSaida = DateTime.UtcNow;
    }

    /// <summary>
    /// RN-011 a RN-013: Altera valor mensal (novo valor usado na próxima compra).
    /// </summary>
    public decimal AlterarValorMensal(decimal novoValor)
    {
        if (novoValor < 100m)
            throw new ArgumentException("O valor mensal mínimo é de R$ 100,00.");

        var valorAnterior = ValorMensal;
        ValorMensal = novoValor;
        return valorAnterior;
    }

    /// <summary>
    /// RN-023/RN-025: Calcula 1/3 do valor mensal para uma data de compra.
    /// </summary>
    public decimal CalcularValorParcela() => Math.Round(ValorMensal / 3m, 2);
}
