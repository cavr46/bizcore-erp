namespace BizCore.Domain.Common;

public class DomainException : Exception
{
    public string Code { get; }
    
    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
    
    public DomainException(string code, string message, Exception innerException) 
        : base(message, innerException)
    {
        Code = code;
    }
}

public class BusinessRuleValidationException : DomainException
{
    public string BrokenRule { get; }
    
    public BusinessRuleValidationException(string brokenRule) 
        : base("BUSINESS_RULE_VIOLATION", $"Business rule violated: {brokenRule}")
    {
        BrokenRule = brokenRule;
    }
}