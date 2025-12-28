using Microsoft.AspNetCore.SignalR;

namespace CarBookingSystem.API.Hubs
{
    public class BookingHub : Hub
    {
        // Notify drivers of a new booking
        public async Task NotifyNewBooking(int bookingId)
        {
            await Clients.All.SendAsync("ReceiveNewBooking", bookingId);
        }

        // Notify customer that their ride is accepted
        public async Task NotifyRideAccepted(int bookingId, int driverId)
        {
            await Clients.All.SendAsync("ReceiveRideAccepted", bookingId, driverId);
        }

        // NEW: Notify that ride is completed
        public async Task NotifyRideCompleted(int bookingId)
        {
            await Clients.All.SendAsync("ReceiveRideCompleted", bookingId);
        }

        // NEW: Notify that payment is done
        public async Task NotifyPaymentDone(int bookingId)
        {
            await Clients.All.SendAsync("ReceivePaymentDone", bookingId);
        }
    }
}