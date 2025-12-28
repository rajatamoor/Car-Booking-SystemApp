using Microsoft.AspNetCore.SignalR.Client;
using CarBookingSystem.Domain;
using CarBookingSystem.Domain.DTOs;
using System.Net.Http.Json;

namespace CarBookingSystem.UI.Services
{
    public class BookingService
    {
        private readonly HttpClient _http;
        private HubConnection _hubConnection;

        public event Action<int> OnNewBooking;
        public event Action<int, int> OnRideAccepted;
        public event Action<int> OnRideCompleted;
        public event Action<int> OnPaymentDone;

        public BookingService(HttpClient http)
        {
            _http = http;
        }

        public async Task InitializeSignalR()
        {
            if (_hubConnection == null)
            {
                var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/');
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{baseUrl}/bookingHub")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<int>("ReceiveNewBooking", (bookingId) =>
                {
                    OnNewBooking?.Invoke(bookingId);
                });

                _hubConnection.On<int, int>("ReceiveRideAccepted", (bookingId, driverId) =>
                {
                    OnRideAccepted?.Invoke(bookingId, driverId);
                });

                _hubConnection.On<int>("ReceiveRideCompleted", (bookingId) =>
                {
                    OnRideCompleted?.Invoke(bookingId);
                });

                _hubConnection.On<int>("ReceivePaymentDone", (bookingId) =>
                {
                    OnPaymentDone?.Invoke(bookingId);
                });

                try
                {
                    await _hubConnection.StartAsync();
                    Console.WriteLine("SignalR Connected!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR Connection Error: {ex.Message}");
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }

        // Location autocomplete
        public async Task<List<LocationSuggestionDto>> GetLocationSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                return new List<LocationSuggestionDto>();

            try
            {
                return await _http.GetFromJsonAsync<List<LocationSuggestionDto>>($"api/customer/locations?query={Uri.EscapeDataString(query)}")
                    ?? new List<LocationSuggestionDto>();
            }
            catch
            {
                return new List<LocationSuggestionDto>();
            }
        }

        // Updated: Book ride using BookRideRequest DTO
        public async Task<BookRideResponse> BookRide(BookRideRequest bookingRequest)
        {
            try
            {
                Console.WriteLine($"Sending booking request to: api/customer/book");
                Console.WriteLine($"Booking data: {System.Text.Json.JsonSerializer.Serialize(bookingRequest)}");

                var response = await _http.PostAsJsonAsync("api/customer/book", bookingRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                    throw new HttpRequestException($"Booking failed: {response.StatusCode} - {errorContent}");
                }

                return await response.Content.ReadFromJsonAsync<BookRideResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in BookRide: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BookingDto>> GetCustomerBookings(int customerId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<BookingDto>>($"api/customer/{customerId}/bookings")
                    ?? new List<BookingDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customer bookings: {ex.Message}");
                return new List<BookingDto>();
            }
        }

        // Driver endpoints (keep existing + add new)
        public async Task<List<BookingDto>> GetPendingRides()
        {
            return await _http.GetFromJsonAsync<List<BookingDto>>("api/driver/pending-rides");
        }

        public async Task AcceptRide(int driverId, int bookingId)
        {
            var response = await _http.PostAsync($"api/driver/{driverId}/accept/{bookingId}", null);
            response.EnsureSuccessStatusCode();
        }

        // Complete ride
        public async Task CompleteRide(int driverId, int bookingId)
        {
            var response = await _http.PostAsync($"api/driver/{driverId}/complete/{bookingId}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<BookingDto>> GetDriverAcceptedRides(int driverId)
        {
            return await _http.GetFromJsonAsync<List<BookingDto>>($"api/driver/{driverId}/accepted-rides");
        }

        // Get completed rides
        public async Task<List<BookingDto>> GetDriverCompletedRides(int driverId)
        {
            return await _http.GetFromJsonAsync<List<BookingDto>>($"api/driver/{driverId}/completed-rides");
        }

        // Admin endpoints
        public async Task<List<BookingDto>> GetAllBookings()
        {
            return await _http.GetFromJsonAsync<List<BookingDto>>("api/admin/bookings");
        }

        public async Task MarkBookingPaid(int bookingId)
        {
            var response = await _http.PostAsync($"api/admin/bookings/{bookingId}/mark-paid", null);
            response.EnsureSuccessStatusCode();
        }
    }
}