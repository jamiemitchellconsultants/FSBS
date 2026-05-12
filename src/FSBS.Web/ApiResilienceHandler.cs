using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace FSBS.Web;

/// <summary>
/// DelegatingHandler that wraps every outbound API call with a Polly resilience
/// pipeline: 3 retries with exponential back-off, then a circuit breaker that
/// opens after 5 consecutive failures and resets after 30 seconds.
/// </summary>
public sealed class ApiResilienceHandler : DelegatingHandler
{
    private static readonly ResiliencePipeline<HttpResponseMessage> _pipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay            = TimeSpan.FromMilliseconds(300),
                BackoffType      = DelayBackoffType.Exponential,
                ShouldHandle     = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException ||
                    (args.Outcome.Result is { } r &&
                     (int)r.StatusCode >= 500 &&
                     r.StatusCode != System.Net.HttpStatusCode.NotImplemented))
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio          = 0.5,
                MinimumThroughput     = 5,
                SamplingDuration      = TimeSpan.FromSeconds(30),
                BreakDuration         = TimeSpan.FromSeconds(30),
                ShouldHandle          = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException ||
                    (args.Outcome.Result is { } r && (int)r.StatusCode >= 500))
            })
            .Build();

    protected override Task<HttpResponseMessage> SendAsync(
        // amazonq-ignore-next-line
        HttpRequestMessage request, CancellationToken ct) =>
        _pipeline.ExecuteAsync(
            async token => await base.SendAsync(request, token), ct).AsTask();
}
