using System;
using System.Collections.Generic;

namespace BizCore.Domain.Common;

public class Address : ValueObject
{
    public string Street1 { get; private set; }
    public string? Street2 { get; private set; }
    public string City { get; private set; }
    public string StateProvince { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private Address() 
    {
        Street1 = string.Empty;
        City = string.Empty;
        StateProvince = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    public Address(
        string street1,
        string city,
        string stateProvince,
        string postalCode,
        string country,
        string? street2 = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(street1))
            throw new ArgumentException("Street1 cannot be empty", nameof(street1));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        Street1 = street1;
        Street2 = street2;
        City = city;
        StateProvince = stateProvince;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
    }

    public string GetFullAddress()
    {
        var parts = new List<string> { Street1 };
        
        if (!string.IsNullOrWhiteSpace(Street2))
            parts.Add(Street2);
        
        parts.Add($"{City}, {StateProvince} {PostalCode}");
        parts.Add(Country);
        
        return string.Join(", ", parts);
    }

    public bool HasCoordinates() => Latitude.HasValue && Longitude.HasValue;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street1;
        yield return Street2;
        yield return City;
        yield return StateProvince;
        yield return PostalCode;
        yield return Country;
    }
}