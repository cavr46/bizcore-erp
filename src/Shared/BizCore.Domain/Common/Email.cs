using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BizCore.Domain.Common;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; private set; }

    private Email()
    {
        Value = string.Empty;
    }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!IsValid(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value;
    }

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public static Email Parse(string value) => new(value);

    public static bool TryParse(string value, out Email? email)
    {
        email = null;

        if (!IsValid(value))
            return false;

        email = new Email(value);
        return true;
    }

    public string GetDomain() => Value.Split('@')[1];
    public string GetLocalPart() => Value.Split('@')[0];

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}