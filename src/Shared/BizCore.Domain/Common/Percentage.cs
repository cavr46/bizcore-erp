using System;
using System.Collections.Generic;

namespace BizCore.Domain.Common;

public class Percentage : ValueObject
{
    public decimal Value { get; private set; }

    private Percentage() { }

    public Percentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100");

        Value = value;
    }

    public static Percentage Zero => new(0);
    public static Percentage OneHundred => new(100);

    public static Percentage FromDecimal(decimal value) => new(value * 100);
    public static Percentage FromFraction(decimal numerator, decimal denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException("Cannot calculate percentage with zero denominator");

        return new((numerator / denominator) * 100);
    }

    public decimal ToDecimal() => Value / 100;
    public decimal ToMultiplier() => 1 + (Value / 100);

    public Money ApplyTo(Money amount) => amount * ToDecimal();
    public decimal ApplyTo(decimal amount) => amount * ToDecimal();

    public Percentage Add(Percentage other) => new(Math.Min(Value + other.Value, 100));
    public Percentage Subtract(Percentage other) => new(Math.Max(Value - other.Value, 0));

    public static Percentage operator +(Percentage left, Percentage right) => left.Add(right);
    public static Percentage operator -(Percentage left, Percentage right) => left.Subtract(right);

    public override string ToString() => $"{Value:F2}%";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}