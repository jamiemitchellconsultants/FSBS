using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.Auth.Commands;

public sealed class ProcessHostedUiCallbackHandler(ICognitoHostedUiService hostedUi)
    : IRequestHandler<ProcessHostedUiCallbackCommand, ProcessHostedUiCallbackResult>
{
    public async Task<ProcessHostedUiCallbackResult> Handle(ProcessHostedUiCallbackCommand request, CancellationToken ct)
    {
        var result = await hostedUi.ProcessCallbackAsync(request.Code, request.State, request.Error, ct);
        return new ProcessHostedUiCallbackResult(
            result.Success,
            result.ErrorCode,
            result.IdToken,
            result.RefreshToken,
            result.ExpiresInSeconds);
    }
}

