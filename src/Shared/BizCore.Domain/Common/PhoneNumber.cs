using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BizCore.Domain.Common;

public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string Value { get; private set; }
    public string? CountryCode { get; private set; }
    public string? Extension { get; private set; }

    private PhoneNumber()
    {
        Value = string.Empty;
    }

    public PhoneNumber(string value, string? countryCode = null, string? extension = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty", nameof(value));

        // Remove common formatting characters
        value = Regex.Replace(value, @"[\s\-\(\)]+", "");

        if (!IsValid(value))
            throw new ArgumentException($"Invalid phone number format: {value}", nameof(value));

        Value = value;
        CountryCode = countryCode;
        Extension = extension;
    }

    public static bool IsValid(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var cleaned = Regex.Replace(phoneNumber, @"[\s\-\(\)]+", "");
        return PhoneRegex.IsMatch(cleaned);
    }

    public string GetFormatted()
    {
        // Basic formatting for display
        if (Value.Length == 10 && !Value.StartsWith("+"))
        {
            // US format: (XXX) XXX-XXXX
            return $"({Value.Substring(0, 3)}) {Value.Substring(3, 3)}-{Value.Substring(6)}";
        }
        
        if (Value.StartsWith("+1") && Value.Length == 12)
        {
            // US format with country code: +1 (XXX) XXX-XXXX
            return $"+1 ({Value.Substring(2, 3)}) {Value.Substring(5, 3)}-{Value.Substring(8)}";
        }

        // Return as-is for other formats
        return Extension != null ? $"{Value} ext. {Extension}" : Value;
    }

    public override string ToString() => GetFormatted();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return CountryCode;
        yield return Extension;
    }
}