namespace Resolvai.Domain.ValueObjects;

/// <summary>
/// Valida dígitos verificadores de CPF e CNPJ, inclusive CNPJ alfanumérico.
/// </summary>
public static class DocumentoValidator
{
    public static bool IsCpfValido(string? value)
    {
        var cpf = (value ?? string.Empty).Trim().Replace(".", string.Empty).Replace("-", string.Empty);
        if (cpf.Length != 11 || cpf.Any(c => !char.IsAsciiDigit(c)) || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        for (var position = 9; position <= 10; position++)
        {
            var sum = 0;
            for (var i = 0; i < position; i++)
            {
                sum += (cpf[i] - '0') * (position + 1 - i);
            }

            var digit = (sum * 10) % 11;
            if (digit == 10)
            {
                digit = 0;
            }

            if (digit != cpf[position] - '0')
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsCnpjValido(string? value)
    {
        var cnpj = (value ?? string.Empty).Trim().Replace(".", string.Empty)
            .Replace("/", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        if (cnpj.Length != 14 || cnpj[..12].Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c)) ||
            cnpj[12..].Any(c => !char.IsAsciiDigit(c)) || cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        static int GetValue(char c) => c - '0'; // ASCII - 48; letras A..Z representam 17..42.
        static int CheckDigit(int sum)
        {
            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum1 = Enumerable.Range(0, 12).Sum(i => GetValue(cnpj[i]) * weights1[i]);
        if (CheckDigit(sum1) != cnpj[12] - '0')
        {
            return false;
        }

        var sum2 = Enumerable.Range(0, 13).Sum(i => GetValue(cnpj[i]) * weights2[i]);
        return CheckDigit(sum2) == cnpj[13] - '0';
    }
}
