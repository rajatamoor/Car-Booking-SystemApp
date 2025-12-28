using CarBookingSystem.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace CarBookingSystemApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            builder.Configuration.AddConfiguration(configuration);

            // Add HttpClient with base address
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                // You might want to change this to your actual API URL
                // For mobile, consider using a configurable base URL
                client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7285");
                client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();

            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            // Add services
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<AdminService>();

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthStateProvider>();

            builder.Services.AddSingleton<ITokenStorage, MauiSecureTokenStorage>();
            builder.Services.AddScoped<IAuthTokenProvider, MauiAuthTokenProvider>();

            return builder.Build();
        }
    }

    // Custom AuthenticationStateProvider for MAUI
    public class MauiAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IAuthTokenProvider _tokenProvider;
        private readonly System.Security.Claims.ClaimsPrincipal _anonymous = new(new System.Security.Claims.ClaimsIdentity());

        public MauiAuthStateProvider(IAuthTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _tokenProvider.GetTokenAsync();

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(_anonymous);
                }

                var claims = ParseClaimsFromJwt(token);
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
                var user = new System.Security.Claims.ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                return new AuthenticationState(_anonymous);
            }
        }

        public void NotifyUserLogin()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private IEnumerable<System.Security.Claims.Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<System.Security.Claims.Claim>();
            var payload = jwt.Split('.')[1];
            
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var jsonBytes = Convert.FromBase64String(payload);
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    // Handle roles explicitly as they might be arrays
                    if (kvp.Value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new System.Security.Claims.Claim(kvp.Key, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new System.Security.Claims.Claim(kvp.Key, kvp.Value.ToString() ?? ""));
                    }
                }
            }
            
            return claims;
        }
    }

    // Interface for token storage
    public interface IAuthTokenProvider
    {
        Task<string> GetTokenAsync();
        Task SetTokenAsync(string token);
        Task ClearTokenAsync();
    }

    public interface ITokenStorage
    {
        Task<string> GetAsync(string key);
        Task SetAsync(string key, string value);
        Task RemoveAsync(string key);
    }

    // MAUI Secure Storage implementation
    public class MauiSecureTokenStorage : ITokenStorage
    {
        public async Task<string> GetAsync(string key)
        {
            return await SecureStorage.Default.GetAsync(key) ?? string.Empty;
        }

        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.Default.SetAsync(key, value);
        }

        public Task RemoveAsync(string key)
        {
            SecureStorage.Default.Remove(key);
            return Task.CompletedTask;
        }
    }

    // MAUI Auth Token Provider
    public class MauiAuthTokenProvider : IAuthTokenProvider
    {
        private readonly ITokenStorage _tokenStorage;
        private const string TokenKey = "auth_token";

        public MauiAuthTokenProvider(ITokenStorage tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        public async Task<string> GetTokenAsync()
        {
            return await _tokenStorage.GetAsync(TokenKey);
        }

        public async Task SetTokenAsync(string token)
        {
            await _tokenStorage.SetAsync(TokenKey, token);
        }

        public async Task ClearTokenAsync()
        {
            await _tokenStorage.RemoveAsync(TokenKey);
        }
    }
}