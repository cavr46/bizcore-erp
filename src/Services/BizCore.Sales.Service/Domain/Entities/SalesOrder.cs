using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Sales.Domain.Entities;

public class SalesOrder : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string OrderNumber { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? RequiredDate { get; private set; }
    public DateTime? ShippedDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public Money SubTotal { get; private set; }
    public Money TaxAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public Money DiscountAmount { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SalesRepId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    
    private readonly List<SalesOrderLine> _lines = new();
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    private SalesOrder() { }

    public SalesOrder(
        Guid tenantId,
        string orderNumber,
        DateTime orderDate,
        Guid customerId,
        string currency)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        OrderNumber = orderNumber;
        OrderDate = orderDate;
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Draft;
        ExchangeRate = 1.0m;
        SubTotal = Money.Zero(currency);
        TaxAmount = Money.Zero(currency);
        TotalAmount = Money.Zero(currency);
        DiscountAmount = Money.Zero(currency);
        
        AddDomainEvent(new SalesOrderCreatedDomainEvent(Id, TenantId, OrderNumber, CustomerId));
    }

    public void AddLine(
        Guid productId,
        string productName,
        decimal quantity,
        string unitOfMeasure,
        Money unitPrice,
        decimal discountPercentage = 0)
    {
        var lineNumber = _lines.Count + 1;
        var line = new SalesOrderLine(
            Id,
            lineNumber,
            productId,
            productName,
            quantity,
            unitOfMeasure,
            unitPrice,
            discountPercentage);
        
        _lines.Add(line);
        RecalculateTotals();
    }

    public void UpdateLine(int lineNumber, decimal quantity, Money unitPrice, decimal discountPercentage = 0)
    {
        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line != null)
        {
            line.UpdateQuantityAndPrice(quantity, unitPrice, discountPercentage);
            RecalculateTotals();
        }
    }

    public void RemoveLine(int lineNumber)
    {
        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line != null)
        {
            _lines.Remove(line);
            RenumberLines();
            RecalculateTotals();
        }
    }

    public void SetRequiredDate(DateTime requiredDate)
    {
        RequiredDate = requiredDate;
    }

    public void SetDiscount(decimal discountPercentage)
    {
        DiscountPercentage = discountPercentage;
        RecalculateTotals();
    }

    public void SetNotes(string notes)
    {
        Notes = notes;
    }

    public void AssignSalesRep(Guid salesRepId)
    {
        SalesRepId = salesRepId;
    }

    public void SetWarehouse(Guid warehouseId)
    {
        WarehouseId = warehouseId;
    }

    public Result Submit()
    {
        if (Status != OrderStatus.Draft)
            return Result.Failure("Only draft orders can be submitted");

        if (!_lines.Any())
            return Result.Failure("Order must have at least one line");

        Status = OrderStatus.Submitted;
        AddDomainEvent(new SalesOrderSubmittedDomainEvent(Id, TenantId, CustomerId));
        
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Submitted)
            return Result.Failure("Only submitted orders can be confirmed");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new SalesOrderConfirmedDomainEvent(Id, TenantId, CustomerId, TotalAmount));
        
        return Result.Success();
    }

    public Result Ship(DateTime shippedDate)
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
        ShippedDate = shippedDate;
        AddDomainEvent(new SalesOrderShippedDomainEvent(Id, TenantId, CustomerId, shippedDate));
        
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Invoiced)
            return Result.Failure("Cannot cancel shipped or invoiced orders");

        Status = OrderStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        AddDomainEvent(new SalesOrderCancelledDomainEvent(Id, TenantId, CustomerId, reason));
        
        return Result.Success();
    }

    private void RecalculateTotals()
    {
        SubTotal = _lines.Aggregate(Money.Zero(Currency), (sum, line) => sum.Add(line.LineTotal));
        DiscountAmount = SubTotal.Multiply(DiscountPercentage / 100);
        TaxAmount = SubTotal.Subtract(DiscountAmount).Multiply(0.1m); // 10% tax
        TotalAmount = SubTotal.Subtract(DiscountAmount).Add(TaxAmount);
    }

    private void RenumberLines()
    {
        var orderedLines = _lines.OrderBy(l => l.LineNumber).ToList();
        for (int i = 0; i < orderedLines.Count; i++)
        {
            orderedLines[i].UpdateLineNumber(i + 1);
        }
    }
}

public class SalesOrderLine : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public Money UnitPrice { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money LineTotal { get; private set; }

    private SalesOrderLine() { }

    public SalesOrderLine(
        Guid orderId,
        int lineNumber,
        Guid productId,
        string productName,
        decimal quantity,
        string unitOfMeasure,
        Money unitPrice,
        decimal discountPercentage)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        LineNumber = lineNumber;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage;
        
        RecalculateLineTotal();
    }

    public void UpdateQuantityAndPrice(decimal quantity, Money unitPrice, decimal discountPercentage)
    {
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage;
        RecalculateLineTotal();
    }

    public void UpdateLineNumber(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    private void RecalculateLineTotal()
    {
        var gross = UnitPrice.Multiply(Quantity);
        DiscountAmount = gross.Multiply(DiscountPercentage / 100);
        LineTotal = gross.Subtract(DiscountAmount);
    }
}

public class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Draft = new(1, nameof(Draft));
    public static readonly OrderStatus Submitted = new(2, nameof(Submitted));
    public static readonly OrderStatus Confirmed = new(3, nameof(Confirmed));
    public static readonly OrderStatus Shipped = new(4, nameof(Shipped));
    public static readonly OrderStatus Invoiced = new(5, nameof(Invoiced));
    public static readonly OrderStatus Cancelled = new(6, nameof(Cancelled));

    private OrderStatus(int value, string name) : base(name, value) { }
}

// Domain Events
public record SalesOrderCreatedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    Guid CustomerId) : INotification;

public record SalesOrderSubmittedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId) : INotification;

public record SalesOrderConfirmedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    Money TotalAmount) : INotification;

public record SalesOrderShippedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    DateTime ShippedDate) : INotification;

public record SalesOrderCancelledDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    string Reason) : INotification;