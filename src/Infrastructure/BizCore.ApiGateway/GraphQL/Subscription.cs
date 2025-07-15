using HotChocolate.Authorization;
using HotChocolate.Subscriptions;

namespace BizCore.ApiGateway.GraphQL;

[Authorize]
public class Subscription
{
    // Real-time notifications for business events
    [Subscribe]
    public async Task<string> OnOrderCreated([EventMessage] string orderId)
    {
        return orderId;
    }

    [Subscribe]
    public async Task<string> OnInventoryChanged([EventMessage] string productId)
    {
        return productId;
    }

    [Subscribe]
    public async Task<string> OnTransactionPosted([EventMessage] string transactionId)
    {
        return transactionId;
    }

    [Subscribe]
    public async Task<string> OnWorkflowCompleted([EventMessage] string workflowId)
    {
        return workflowId;
    }
}