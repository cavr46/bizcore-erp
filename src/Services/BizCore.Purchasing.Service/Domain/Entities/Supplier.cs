using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Purchasing.Domain.Entities;

public class Supplier : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string SupplierNumber { get; private set; }
    public string Name { get; private set; }
    public string? LegalName { get; private set; }
    public string? TaxId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public SupplierType Type { get; private set; }
    public SupplierStatus Status { get; private set; }
    public string Currency { get; private set; }
    public int PaymentTerms { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public SupplierRating Rating { get; private set; }
    public DateTime? LastOrderDate { get; private set; }
    public DateTime? LastPaymentDate { get; private set; }
    public Guid? BuyerId { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }
    
    private readonly List<SupplierAddress> _addresses = new();
    public IReadOnlyCollection<SupplierAddress> Addresses => _addresses.AsReadOnly();
    
    private readonly List<SupplierContact> _contacts = new();
    public IReadOnlyCollection<SupplierContact> Contacts => _contacts.AsReadOnly();
    
    private readonly List<SupplierCertification> _certifications = new();
    public IReadOnlyCollection<SupplierCertification> Certifications => _certifications.AsReadOnly();
    
    private readonly List<SupplierPerformanceMetric> _performanceMetrics = new();
    public IReadOnlyCollection<SupplierPerformanceMetric> PerformanceMetrics => _performanceMetrics.AsReadOnly();

    private Supplier() { }

    public Supplier(
        Guid tenantId,
        string supplierNumber,
        string name,
        SupplierType type,
        string currency)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        SupplierNumber = supplierNumber;
        Name = name;
        Type = type;
        Currency = currency;
        Status = SupplierStatus.Active;
        Rating = SupplierRating.NotRated;
        PaymentTerms = 30;
        IsActive = true;
        
        AddDomainEvent(new SupplierCreatedDomainEvent(Id, TenantId, SupplierNumber, Name));
    }

    public void UpdateBasicInfo(string name, string? legalName, string? taxId, string? email, string? phone, string? website)
    {
        Name = name;
        LegalName = legalName;
        TaxId = taxId;
        Email = email;
        Phone = phone;
        Website = website;
    }

    public void SetCreditLimit(decimal creditLimit)
    {
        CreditLimit = creditLimit;
    }

    public void UpdateBalance(decimal amount)
    {
        CurrentBalance += amount;
    }

    public void SetPaymentTerms(int days)
    {
        PaymentTerms = days;
    }

    public void AssignBuyer(Guid buyerId)
    {
        BuyerId = buyerId;
    }

    public void SetPreferred(bool isPreferred)
    {
        IsPreferred = isPreferred;
    }

    public void UpdateRating(SupplierRating rating)
    {
        Rating = rating;
        AddDomainEvent(new SupplierRatingUpdatedDomainEvent(Id, TenantId, rating));
    }

    public void AddAddress(string type, string street, string city, string state, string postalCode, string country)
    {
        var address = new SupplierAddress(Id, type, street, city, state, postalCode, country);
        _addresses.Add(address);
    }

    public void AddContact(string name, string? title, string? email, string? phone, bool isPrimary = false)
    {
        if (isPrimary)
        {
            // Remove primary flag from other contacts
            foreach (var contact in _contacts)
            {
                contact.SetPrimary(false);
            }
        }

        var supplierContact = new SupplierContact(Id, name, title, email, phone, isPrimary);
        _contacts.Add(supplierContact);
    }

    public void AddCertification(string name, string? issuedBy, DateTime issuedDate, DateTime? expiryDate)
    {
        var certification = new SupplierCertification(Id, name, issuedBy, issuedDate, expiryDate);
        _certifications.Add(certification);
    }

    public void UpdatePerformanceMetric(string metricName, decimal value, DateTime periodStart, DateTime periodEnd)
    {
        var metric = _performanceMetrics.FirstOrDefault(m => 
            m.MetricName == metricName && 
            m.PeriodStart == periodStart && 
            m.PeriodEnd == periodEnd);

        if (metric == null)
        {
            metric = new SupplierPerformanceMetric(Id, metricName, value, periodStart, periodEnd);
            _performanceMetrics.Add(metric);
        }
        else
        {
            metric.UpdateValue(value);
        }
    }

    public void RecordOrder(DateTime orderDate, decimal orderAmount)
    {
        LastOrderDate = orderDate;
        // Update performance metrics
        AddDomainEvent(new SupplierOrderRecordedDomainEvent(Id, TenantId, orderDate, orderAmount));
    }

    public void RecordPayment(DateTime paymentDate, decimal paymentAmount)
    {
        LastPaymentDate = paymentDate;
        CurrentBalance -= paymentAmount;
        AddDomainEvent(new SupplierPaymentRecordedDomainEvent(Id, TenantId, paymentDate, paymentAmount));
    }

    public void Activate() => Status = SupplierStatus.Active;
    public void Deactivate() => Status = SupplierStatus.Inactive;
    public void Block() => Status = SupplierStatus.Blocked;
    public void Suspend() => Status = SupplierStatus.Suspended;

    public bool CanPlaceOrder(decimal orderAmount)
    {
        return Status == SupplierStatus.Active && 
               IsActive && 
               (CreditLimit == 0 || CurrentBalance + orderAmount <= CreditLimit);
    }

    public decimal GetAveragePerformanceScore()
    {
        if (!_performanceMetrics.Any())
            return 0;

        var recentMetrics = _performanceMetrics
            .Where(m => m.PeriodEnd >= DateTime.UtcNow.AddMonths(-3))
            .ToList();

        if (!recentMetrics.Any())
            return 0;

        return recentMetrics.Average(m => m.Value);
    }
}

public class SupplierAddress : Entity<Guid>
{
    public Guid SupplierId { get; private set; }
    public string Type { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; }

    private SupplierAddress() { }

    public SupplierAddress(Guid supplierId, string type, string street, string city, string state, string postalCode, string country)
    {
        Id = Guid.NewGuid();
        SupplierId = supplierId;
        Type = type;
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }
}

public class SupplierContact : Entity<Guid>
{
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; }
    public string? Title { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }

    private SupplierContact() { }

    public SupplierContact(Guid supplierId, string name, string? title, string? email, string? phone, bool isPrimary)
    {
        Id = Guid.NewGuid();
        SupplierId = supplierId;
        Name = name;
        Title = title;
        Email = email;
        Phone = phone;
        IsPrimary = isPrimary;
        IsActive = true;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class SupplierCertification : Entity<Guid>
{
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; }
    public string? IssuedBy { get; private set; }
    public DateTime IssuedDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsActive { get; private set; }

    private SupplierCertification() { }

    public SupplierCertification(Guid supplierId, string name, string? issuedBy, DateTime issuedDate, DateTime? expiryDate)
    {
        Id = Guid.NewGuid();
        SupplierId = supplierId;
        Name = name;
        IssuedBy = issuedBy;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        IsActive = true;
    }

    public bool IsExpired()
    {
        return ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    }

    public bool IsExpiringSoon(int daysWarning = 30)
    {
        return ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow.AddDays(daysWarning);
    }
}

public class SupplierPerformanceMetric : Entity<Guid>
{
    public Guid SupplierId { get; private set; }
    public string MetricName { get; private set; }
    public decimal Value { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private SupplierPerformanceMetric() { }

    public SupplierPerformanceMetric(Guid supplierId, string metricName, decimal value, DateTime periodStart, DateTime periodEnd)
    {
        Id = Guid.NewGuid();
        SupplierId = supplierId;
        MetricName = metricName;
        Value = value;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RecordedAt = DateTime.UtcNow;
    }

    public void UpdateValue(decimal value)
    {
        Value = value;
        RecordedAt = DateTime.UtcNow;
    }
}

public class SupplierType : SmartEnum<SupplierType>
{
    public static readonly SupplierType Manufacturer = new(1, nameof(Manufacturer));
    public static readonly SupplierType Distributor = new(2, nameof(Distributor));
    public static readonly SupplierType ServiceProvider = new(3, nameof(ServiceProvider));
    public static readonly SupplierType Contractor = new(4, nameof(Contractor));
    public static readonly SupplierType Consultant = new(5, nameof(Consultant));

    private SupplierType(int value, string name) : base(name, value) { }
}

public class SupplierStatus : SmartEnum<SupplierStatus>
{
    public static readonly SupplierStatus Active = new(1, nameof(Active));
    public static readonly SupplierStatus Inactive = new(2, nameof(Inactive));
    public static readonly SupplierStatus Blocked = new(3, nameof(Blocked));
    public static readonly SupplierStatus Suspended = new(4, nameof(Suspended));
    public static readonly SupplierStatus PendingApproval = new(5, nameof(PendingApproval));

    private SupplierStatus(int value, string name) : base(name, value) { }
}

public class SupplierRating : SmartEnum<SupplierRating>
{
    public static readonly SupplierRating NotRated = new(0, nameof(NotRated));
    public static readonly SupplierRating Poor = new(1, nameof(Poor));
    public static readonly SupplierRating Fair = new(2, nameof(Fair));
    public static readonly SupplierRating Good = new(3, nameof(Good));
    public static readonly SupplierRating VeryGood = new(4, nameof(VeryGood));
    public static readonly SupplierRating Excellent = new(5, nameof(Excellent));

    private SupplierRating(int value, string name) : base(name, value) { }
}

// Domain Events
public record SupplierCreatedDomainEvent(
    Guid SupplierId,
    Guid TenantId,
    string SupplierNumber,
    string Name) : INotification;

public record SupplierRatingUpdatedDomainEvent(
    Guid SupplierId,
    Guid TenantId,
    SupplierRating NewRating) : INotification;

public record SupplierOrderRecordedDomainEvent(
    Guid SupplierId,
    Guid TenantId,
    DateTime OrderDate,
    decimal OrderAmount) : INotification;

public record SupplierPaymentRecordedDomainEvent(
    Guid SupplierId,
    Guid TenantId,
    DateTime PaymentDate,
    decimal PaymentAmount) : INotification;