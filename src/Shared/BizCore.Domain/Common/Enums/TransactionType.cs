namespace BizCore.Domain.Common.Enums;

public enum TransactionType
{
    Debit = 1,
    Credit = 2
}

public enum InventoryTransactionType
{
    Purchase = 1,
    Sale = 2,
    Transfer = 3,
    Adjustment = 4,
    Production = 5,
    Return = 6,
    Damage = 7,
    StockTake = 8,
    Consumption = 9,
    Assembly = 10,
    Disassembly = 11
}

public enum PaymentMethod
{
    Cash = 1,
    Check = 2,
    CreditCard = 3,
    DebitCard = 4,
    BankTransfer = 5,
    PayPal = 6,
    Stripe = 7,
    MercadoPago = 8,
    Bitcoin = 9,
    Other = 99
}

public enum TaxType
{
    Sales = 1,
    Purchase = 2,
    VAT = 3,
    GST = 4,
    Withholding = 5,
    Customs = 6,
    Excise = 7,
    Other = 99
}