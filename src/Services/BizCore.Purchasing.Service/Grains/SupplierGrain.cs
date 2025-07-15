using BizCore.Purchasing.Domain.Entities;
using BizCore.Orleans.Core;
using Microsoft.Extensions.Logging;

namespace BizCore.Purchasing.Grains;

public interface ISupplierGrain : IGrainWithGuidKey
{
    Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request);
    Task<Supplier> GetSupplierAsync();
    Task UpdateBasicInfoAsync(UpdateSupplierBasicInfoRequest request);
    Task SetCreditLimitAsync(decimal creditLimit);
    Task UpdateBalanceAsync(decimal amount);
    Task SetPaymentTermsAsync(int days);
    Task AssignBuyerAsync(Guid buyerId);
    Task SetPreferredAsync(bool isPreferred);
    Task UpdateRatingAsync(SupplierRating rating);
    Task AddAddressAsync(AddSupplierAddressRequest request);
    Task AddContactAsync(AddSupplierContactRequest request);
    Task AddCertificationAsync(AddSupplierCertificationRequest request);
    Task UpdatePerformanceMetricAsync(UpdatePerformanceMetricRequest request);
    Task RecordOrderAsync(DateTime orderDate, decimal orderAmount);
    Task RecordPaymentAsync(DateTime paymentDate, decimal paymentAmount);
    Task ActivateAsync();
    Task DeactivateAsync();
    Task BlockAsync();
    Task SuspendAsync();
    Task<bool> CanPlaceOrderAsync(decimal orderAmount);
    Task<decimal> GetAveragePerformanceScoreAsync();
    Task<SupplierPerformanceReport> GetPerformanceReportAsync();
}

public class SupplierGrain : TenantGrainBase<SupplierState>, ISupplierGrain
{
    private readonly ILogger<SupplierGrain> _logger;

    public SupplierGrain(ILogger<SupplierGrain> logger)
    {
        _logger = logger;
    }

    public async Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request)
    {
        if (_state.State.Supplier != null)
            throw new InvalidOperationException("Supplier already exists");

        var supplier = new Supplier(
            GetTenantId(),
            request.SupplierNumber,
            request.Name,
            request.Type,
            request.Currency);

        if (!string.IsNullOrEmpty(request.LegalName))
            supplier.UpdateBasicInfo(request.Name, request.LegalName, request.TaxId, request.Email, request.Phone, request.Website);

        if (request.CreditLimit.HasValue)
            supplier.SetCreditLimit(request.CreditLimit.Value);

        if (request.PaymentTerms.HasValue)
            supplier.SetPaymentTerms(request.PaymentTerms.Value);

        _state.State.Supplier = supplier;
        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} created for tenant {TenantId}", 
            request.SupplierNumber, GetTenantId());

        return supplier;
    }

    public Task<Supplier> GetSupplierAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        return Task.FromResult(_state.State.Supplier);
    }

    public async Task UpdateBasicInfoAsync(UpdateSupplierBasicInfoRequest request)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.UpdateBasicInfo(
            request.Name,
            request.LegalName,
            request.TaxId,
            request.Email,
            request.Phone,
            request.Website);

        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} basic info updated for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task SetCreditLimitAsync(decimal creditLimit)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.SetCreditLimit(creditLimit);
        await SaveStateAsync();

        _logger.LogInformation("Credit limit set to {CreditLimit} for supplier {SupplierNumber} for tenant {TenantId}", 
            creditLimit, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task UpdateBalanceAsync(decimal amount)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.UpdateBalance(amount);
        await SaveStateAsync();

        _logger.LogInformation("Balance updated by {Amount} for supplier {SupplierNumber} for tenant {TenantId}", 
            amount, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task SetPaymentTermsAsync(int days)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.SetPaymentTerms(days);
        await SaveStateAsync();

        _logger.LogInformation("Payment terms set to {Days} days for supplier {SupplierNumber} for tenant {TenantId}", 
            days, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task AssignBuyerAsync(Guid buyerId)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.AssignBuyer(buyerId);
        await SaveStateAsync();

        _logger.LogInformation("Buyer {BuyerId} assigned to supplier {SupplierNumber} for tenant {TenantId}", 
            buyerId, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task SetPreferredAsync(bool isPreferred)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.SetPreferred(isPreferred);
        await SaveStateAsync();

        _logger.LogInformation("Preferred status set to {IsPreferred} for supplier {SupplierNumber} for tenant {TenantId}", 
            isPreferred, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task UpdateRatingAsync(SupplierRating rating)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.UpdateRating(rating);
        await SaveStateAsync();

        _logger.LogInformation("Rating updated to {Rating} for supplier {SupplierNumber} for tenant {TenantId}", 
            rating, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task AddAddressAsync(AddSupplierAddressRequest request)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.AddAddress(
            request.Type,
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.Country);

        await SaveStateAsync();

        _logger.LogInformation("Address added to supplier {SupplierNumber} for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task AddContactAsync(AddSupplierContactRequest request)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.AddContact(
            request.Name,
            request.Title,
            request.Email,
            request.Phone,
            request.IsPrimary);

        await SaveStateAsync();

        _logger.LogInformation("Contact {ContactName} added to supplier {SupplierNumber} for tenant {TenantId}", 
            request.Name, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task AddCertificationAsync(AddSupplierCertificationRequest request)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.AddCertification(
            request.Name,
            request.IssuedBy,
            request.IssuedDate,
            request.ExpiryDate);

        await SaveStateAsync();

        _logger.LogInformation("Certification {CertificationName} added to supplier {SupplierNumber} for tenant {TenantId}", 
            request.Name, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task UpdatePerformanceMetricAsync(UpdatePerformanceMetricRequest request)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.UpdatePerformanceMetric(
            request.MetricName,
            request.Value,
            request.PeriodStart,
            request.PeriodEnd);

        await SaveStateAsync();

        _logger.LogInformation("Performance metric {MetricName} updated for supplier {SupplierNumber} for tenant {TenantId}", 
            request.MetricName, _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task RecordOrderAsync(DateTime orderDate, decimal orderAmount)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.RecordOrder(orderDate, orderAmount);
        await SaveStateAsync();

        _logger.LogInformation("Order recorded for supplier {SupplierNumber} for tenant {TenantId}: {OrderAmount} on {OrderDate}", 
            _state.State.Supplier.SupplierNumber, GetTenantId(), orderAmount, orderDate);
    }

    public async Task RecordPaymentAsync(DateTime paymentDate, decimal paymentAmount)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.RecordPayment(paymentDate, paymentAmount);
        await SaveStateAsync();

        _logger.LogInformation("Payment recorded for supplier {SupplierNumber} for tenant {TenantId}: {PaymentAmount} on {PaymentDate}", 
            _state.State.Supplier.SupplierNumber, GetTenantId(), paymentAmount, paymentDate);
    }

    public async Task ActivateAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.Activate();
        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} activated for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task DeactivateAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.Deactivate();
        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} deactivated for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task BlockAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.Block();
        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} blocked for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public async Task SuspendAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        _state.State.Supplier.Suspend();
        await SaveStateAsync();

        _logger.LogInformation("Supplier {SupplierNumber} suspended for tenant {TenantId}", 
            _state.State.Supplier.SupplierNumber, GetTenantId());
    }

    public Task<bool> CanPlaceOrderAsync(decimal orderAmount)
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var canPlace = _state.State.Supplier.CanPlaceOrder(orderAmount);
        return Task.FromResult(canPlace);
    }

    public Task<decimal> GetAveragePerformanceScoreAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var score = _state.State.Supplier.GetAveragePerformanceScore();
        return Task.FromResult(score);
    }

    public Task<SupplierPerformanceReport> GetPerformanceReportAsync()
    {
        if (_state.State.Supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var supplier = _state.State.Supplier;
        var report = new SupplierPerformanceReport
        {
            SupplierId = supplier.Id,
            SupplierNumber = supplier.SupplierNumber,
            Name = supplier.Name,
            Rating = supplier.Rating,
            AveragePerformanceScore = supplier.GetAveragePerformanceScore(),
            CurrentBalance = supplier.CurrentBalance,
            CreditLimit = supplier.CreditLimit,
            PaymentTerms = supplier.PaymentTerms,
            LastOrderDate = supplier.LastOrderDate,
            LastPaymentDate = supplier.LastPaymentDate,
            IsPreferred = supplier.IsPreferred,
            Status = supplier.Status,
            CertificationCount = supplier.Certifications.Count,
            ActiveCertifications = supplier.Certifications.Count(c => c.IsActive && !c.IsExpired()),
            ExpiringSoonCertifications = supplier.Certifications.Count(c => c.IsExpiringSoon()),
            PerformanceMetrics = supplier.PerformanceMetrics.Select(pm => new PerformanceMetricSummary
            {
                MetricName = pm.MetricName,
                Value = pm.Value,
                PeriodStart = pm.PeriodStart,
                PeriodEnd = pm.PeriodEnd,
                RecordedAt = pm.RecordedAt
            }).ToList()
        };

        return Task.FromResult(report);
    }
}

[GenerateSerializer]
public class SupplierState
{
    [Id(0)]
    public Supplier? Supplier { get; set; }
}

// Request DTOs
public record CreateSupplierRequest(
    string SupplierNumber,
    string Name,
    SupplierType Type,
    string Currency,
    string? LegalName = null,
    string? TaxId = null,
    string? Email = null,
    string? Phone = null,
    string? Website = null,
    decimal? CreditLimit = null,
    int? PaymentTerms = null);

public record UpdateSupplierBasicInfoRequest(
    string Name,
    string? LegalName = null,
    string? TaxId = null,
    string? Email = null,
    string? Phone = null,
    string? Website = null);

public record AddSupplierAddressRequest(
    string Type,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record AddSupplierContactRequest(
    string Name,
    string? Title = null,
    string? Email = null,
    string? Phone = null,
    bool IsPrimary = false);

public record AddSupplierCertificationRequest(
    string Name,
    string? IssuedBy = null,
    DateTime IssuedDate = default,
    DateTime? ExpiryDate = null);

public record UpdatePerformanceMetricRequest(
    string MetricName,
    decimal Value,
    DateTime PeriodStart,
    DateTime PeriodEnd);

public record SupplierPerformanceReport
{
    public Guid SupplierId { get; init; }
    public string SupplierNumber { get; init; }
    public string Name { get; init; }
    public SupplierRating Rating { get; init; }
    public decimal AveragePerformanceScore { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal CreditLimit { get; init; }
    public int PaymentTerms { get; init; }
    public DateTime? LastOrderDate { get; init; }
    public DateTime? LastPaymentDate { get; init; }
    public bool IsPreferred { get; init; }
    public SupplierStatus Status { get; init; }
    public int CertificationCount { get; init; }
    public int ActiveCertifications { get; init; }
    public int ExpiringSoonCertifications { get; init; }
    public List<PerformanceMetricSummary> PerformanceMetrics { get; init; } = new();
}

public record PerformanceMetricSummary
{
    public string MetricName { get; init; }
    public decimal Value { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime RecordedAt { get; init; }
}