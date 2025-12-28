using System.Net.Http.Json;
using CarBookingSystem.Domain;

namespace CarBookingSystem.UI.Services
{
    public class AdminService
    {
        private readonly HttpClient _http;

        public AdminService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<User>> GetDrivers()
        {
            var result = await _http.GetFromJsonAsync<List<User>>("api/admin/drivers");
            return result ?? new List<User>();
        }

        public async Task<User> AddDriver(User driver)
        {
            var response = await _http.PostAsJsonAsync("api/admin/drivers", driver);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task DeleteDriver(int id)
        {
            var response = await _http.DeleteAsync($"api/admin/drivers/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Car>> GetCars()
        {
            var result = await _http.GetFromJsonAsync<List<Car>>("api/admin/cars");
            return result ?? new List<Car>();
        }

        public async Task<Car> AddCar(Car car)
        {
            var response = await _http.PostAsJsonAsync("api/admin/cars", car);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Car>();
        }

        public async Task DeleteCar(int id)
        {
            var response = await _http.DeleteAsync($"api/admin/cars/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}