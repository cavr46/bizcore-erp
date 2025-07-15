namespace BizCore.Application.Common.Interfaces;

public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}