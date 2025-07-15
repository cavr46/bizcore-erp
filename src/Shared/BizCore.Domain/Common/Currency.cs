using System;
using System.Collections.Generic;

namespace BizCore.Domain.Common;

public class Currency : ValueObject
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Symbol { get; private set; }
    public int DecimalPlaces { get; private set; }

    private Currency()
    {
        Code = string.Empty;
        Name = string.Empty;
        Symbol = string.Empty;
    }

    public Currency(string code, string name, string symbol, int decimalPlaces = 2)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Currency code must be 3 characters", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Currency symbol cannot be empty", nameof(symbol));
        if (decimalPlaces < 0 || decimalPlaces > 4)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be between 0 and 4");

        Code = code.ToUpperInvariant();
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
    }

    // Common currencies
    public static Currency USD => new("USD", "US Dollar", "$", 2);
    public static Currency EUR => new("EUR", "Euro", "€", 2);
    public static Currency GBP => new("GBP", "British Pound", "£", 2);
    public static Currency JPY => new("JPY", "Japanese Yen", "¥", 0);
    public static Currency CHF => new("CHF", "Swiss Franc", "Fr", 2);
    public static Currency CAD => new("CAD", "Canadian Dollar", "C$", 2);
    public static Currency AUD => new("AUD", "Australian Dollar", "A$", 2);
    public static Currency CNY => new("CNY", "Chinese Yuan", "¥", 2);
    public static Currency MXN => new("MXN", "Mexican Peso", "$", 2);
    public static Currency BRL => new("BRL", "Brazilian Real", "R$", 2);
    public static Currency ARS => new("ARS", "Argentine Peso", "$", 2);
    public static Currency CLP => new("CLP", "Chilean Peso", "$", 0);
    public static Currency COP => new("COP", "Colombian Peso", "$", 0);
    public static Currency PEN => new("PEN", "Peruvian Sol", "S/", 2);

    public static Currency FromCode(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "USD" => USD,
            "EUR" => EUR,
            "GBP" => GBP,
            "JPY" => JPY,
            "CHF" => CHF,
            "CAD" => CAD,
            "AUD" => AUD,
            "CNY" => CNY,
            "MXN" => MXN,
            "BRL" => BRL,
            "ARS" => ARS,
            "CLP" => CLP,
            "COP" => COP,
            "PEN" => PEN,
            _ => throw new ArgumentException($"Unknown currency code: {code}")
        };
    }

    public decimal Round(decimal amount) => Math.Round(amount, DecimalPlaces);

    public override string ToString() => Code;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}