using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using CarBookingSystemApp;
using CarBookingSystem.Domain;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace CarBookingSystem.UI.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _navManager;
        private readonly IAuthTokenProvider _tokenProvider;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<AuthService> _logger;

        public event Action OnAuthStateChanged;

        public string Token { get; private set; } = string.Empty;
        public string Role { get; private set; } = string.Empty;
        public string UserName { get; private set; } = string.Empty;
        public string UserEmail { get; private set; } = string.Empty;
        public int UserId { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public AuthService(HttpClient http, NavigationManager navManager,
                         IAuthTokenProvider tokenProvider,
                         AuthenticationStateProvider authStateProvider,
                         ILogger<AuthService> logger)
        {
            _http = http;
            _navManager = navManager;
            _tokenProvider = tokenProvider;
            _authStateProvider = authStateProvider;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Token = await _tokenProvider.GetTokenAsync() ?? string.Empty;
                _logger.LogInformation($"AuthService InitializeAsync called. Token exists: {!string.IsNullOrEmpty(Token)}");

                if (!string.IsNullOrEmpty(Token))
                {
                    await LoadUserFromToken();
                }
                else
                {
                    _logger.LogWarning("No token found during initialization");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Auth initialization error: {ex.Message}");
                Token = string.Empty;
            }
        }

        public async Task<bool> Login(string email, string password)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {email}");

                var loginRequest = new { Email = email, Password = password };
                var response = await _http.PostAsJsonAsync("api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _logger.LogInformation($"Login response successful for {email}");

                    if (result.TryGetProperty("token", out var tokenElement))
                    {
                        Token = tokenElement.GetString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(Token))
                        {
                            await _tokenProvider.SetTokenAsync(Token);
                            await LoadUserFromToken();

                            OnAuthStateChanged?.Invoke();

                            // Notify authentication state changed
                            if (_authStateProvider is MauiAuthStateProvider mauiAuthProvider)
                            {
                                mauiAuthProvider.NotifyUserLogin();
                            }

                            _logger.LogInformation($"Login successful for user {UserName} (ID: {UserId})");
                            return true;
                        }
                    }
                    else
                    {
                        _logger.LogError("Token not found in login response");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Login failed with status {response.StatusCode}: {errorContent}");
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Login error for {email}: {ex.Message}");
                return false;
            }
        }

        public async Task Register(User user)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", user);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation($"Registration successful for {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Registration error: {ex.Message}");
                throw;
            }
        }

        public async Task Logout()
        {
            Token = string.Empty;
            Role = string.Empty;
            UserName = string.Empty;
            UserEmail = string.Empty;
            UserId = 0;

            await _tokenProvider.ClearTokenAsync();

            // Notify authentication state changed
            if (_authStateProvider is MauiAuthStateProvider mauiAuthProvider)
            {
                mauiAuthProvider.NotifyUserLogout();
            }

            OnAuthStateChanged?.Invoke();

            _logger.LogInformation("User logged out");
            _navManager.NavigateTo("/", true);
        }

        private async Task LoadUserFromToken()
        {
            if (string.IsNullOrEmpty(Token))
            {
                _logger.LogWarning("LoadUserFromToken called with empty token");
                return;
            }

            try
            {
                _logger.LogInformation($"Loading user from token. Token length: {Token.Length}");

                var handler = new JwtSecurityTokenHandler();

                if (handler.CanReadToken(Token))
                {
                    var jwtToken = handler.ReadJwtToken(Token);

                    _logger.LogInformation($"Token parsed successfully. Claims found:");
                    foreach (var claim in jwtToken.Claims)
                    {
                        _logger.LogInformation($"  Claim: {claim.Type} = {claim.Value}");
                    }

                    // Try multiple ways to get user ID
                    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value
                        ?? "0";

                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        UserId = userId;
                        _logger.LogInformation($"User ID parsed successfully: {UserId}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to parse User ID from claim value: {userIdClaim}");
                        UserId = 0;
                    }

                    // Get user name
                    UserName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                        ?? string.Empty;

                    // Get user email
                    UserEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                        ?? string.Empty;

                    // Get role
                    Role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                        ?? string.Empty;

                    _logger.LogInformation($"User loaded from token - ID: {UserId}, Name: {UserName}, Email: {UserEmail}, Role: {Role}");
                }
                else
                {
                    _logger.LogError("Invalid JWT token format - cannot read token");
                    await Logout();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading token: {ex.Message}");
                await Logout();
            }
        }

        public ClaimsPrincipal GetClaimsPrincipal()
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(Role) || UserId <= 0)
            {
                _logger.LogWarning("GetClaimsPrincipal called but user is not properly authenticated");
                return new ClaimsPrincipal();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim("userId", UserId.ToString()),
                new Claim(ClaimTypes.Name, UserName),
                new Claim(ClaimTypes.Email, UserEmail),
                new Claim(ClaimTypes.Role, Role)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }

        // Helper method to get user ID from current authentication state
        public async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                if (UserId > 0)
                {
                    return UserId;
                }

                // Try to refresh from token
                Token = await _tokenProvider.GetTokenAsync() ?? string.Empty;
                if (!string.IsNullOrEmpty(Token))
                {
                    await LoadUserFromToken();
                    return UserId;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return 0;
            }
        }
    }
}