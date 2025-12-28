using System.Net.Http.Headers;
using CarBookingSystemApp;

namespace CarBookingSystem.UI.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthTokenProvider _tokenProvider;

    public AuthHeaderHandler(IAuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
