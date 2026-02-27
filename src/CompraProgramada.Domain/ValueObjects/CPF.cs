namespace CompraProgramada.Domain.ValueObjects;

public sealed class CPF : IEquatable<CPF>
{
    public string Numero { get; }

    private CPF(string numero)
    {
        Numero = numero;
    }

    public static CPF Criar(string numero)
    {
        var apenasDigitos = new string(numero?.Where(char.IsDigit).ToArray() ?? []);

        if (apenasDigitos.Length != 11)
            throw new ArgumentException("CPF deve conter exatamente 11 dígitos.");

        if (apenasDigitos.Distinct().Count() == 1)
            throw new ArgumentException("CPF inválido.");

        if (!ValidarDigitosVerificadores(apenasDigitos))
            throw new ArgumentException("CPF inválido.");

        return new CPF(apenasDigitos);
    }

    private static bool ValidarDigitosVerificadores(string cpf)
    {
        var multiplicadores1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicadores2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var digitos = cpf[..9];
        var soma = digitos.Select((c, i) => (c - '0') * multiplicadores1[i]).Sum();
        var resto = soma % 11;
        var primeiroDigito = resto < 2 ? 0 : 11 - resto;

        if ((cpf[9] - '0') != primeiroDigito)
            return false;

        digitos = cpf[..10];
        soma = digitos.Select((c, i) => (c - '0') * multiplicadores2[i]).Sum();
        resto = soma % 11;
        var segundoDigito = resto < 2 ? 0 : 11 - resto;

        return (cpf[10] - '0') == segundoDigito;
    }

    public bool Equals(CPF? other) => other is not null && Numero == other.Numero;
    public override bool Equals(object? obj) => Equals(obj as CPF);
    public override int GetHashCode() => Numero.GetHashCode();
    public override string ToString() => Numero;

    public static bool operator ==(CPF? left, CPF? right) => Equals(left, right);
    public static bool operator !=(CPF? left, CPF? right) => !Equals(left, right);
}
