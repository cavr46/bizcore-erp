namespace BizCore.Domain.Common.Enums;

public enum DocumentStatus
{
    Draft = 0,
    Pending = 1,
    InProgress = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Completed = 6,
    Cancelled = 7,
    OnHold = 8,
    Expired = 9,
    Archived = 10
}

public enum ApprovalStatus
{
    NotRequired = 0,
    Pending = 1,
    InProgress = 2,
    PartiallyApproved = 3,
    Approved = 4,
    Rejected = 5,
    Escalated = 6
}

public enum PaymentStatus
{
    NotApplicable = 0,
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    Refunded = 6,
    Failed = 7
}

public enum DeliveryStatus
{
    NotApplicable = 0,
    Pending = 1,
    Processing = 2,
    Ready = 3,
    InTransit = 4,
    PartiallyDelivered = 5,
    Delivered = 6,
    Failed = 7,
    Returned = 8
}