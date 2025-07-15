using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BizCore.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestGuid = Guid.NewGuid();
        var requestNameWithGuid = $"{requestName} [{requestGuid}]";

        _logger.LogInformation("Starting request {RequestNameWithGuid}", requestNameWithGuid);

        TResponse response;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            response = await next();
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Completed request {RequestNameWithGuid} in {ElapsedMilliseconds}ms", 
                requestNameWithGuid, 
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}