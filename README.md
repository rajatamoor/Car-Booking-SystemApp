Car Booking System
Project Overview
A comprehensive Car Booking System built with .NET MAUI Hybrid and ASP.NET Core Web API, featuring role-based authentication and a complete booking management solution for customers, drivers, and administrators.

✨ Features
👥 Multi-Role System
Customers: Book rides, view booking history, track rides

Drivers: Accept/reject rides, update ride status, view earnings

Admins: Manage all bookings, view system analytics, manage users

🔐 Authentication & Security
JWT-based authentication with secure token storage

Role-based authorization

Secure password handling

MAUI Secure Storage for token management

🚗 Ride Management
Real-time ride booking

Ride status tracking (Pending, Accepted, In Progress, Completed)

Driver assignment system

Booking history and receipts

📱 Cross-Platform Support
.NET MAUI Hybrid app (Windows, macOS, Android, iOS)

Responsive Blazor UI

Native mobile experience with web technologies

🛠 Technology Stack
Backend
ASP.NET Core 8 - Web API

Entity Framework Core - Database ORM

SQL Server - Database

JWT Authentication - Secure token-based auth

Swagger/OpenAPI - API documentation

Frontend
.NET MAUI Blazor Hybrid - Cross-platform UI

Blazor Components - Reusable UI components

Bootstrap 5 - Styling framework

MAUI Secure Storage - Secure local storage

Architecture
Clean Architecture pattern

Repository pattern with Entity Framework

Dependency Injection

Async/Await pattern throughout

📁 Project Structure
text
CarBookingSystem/
├── CarBookingSystem.API/          # ASP.NET Core Web API
│   ├── Controllers/               # API Controllers
│   ├── Services/                  # Business logic services
│   ├── Data/                      # Data models and DbContext
│   └── Program.cs                 # Startup configuration
│
├── CarBookingSystem.Domain/       # Domain models and entities
│   ├── Entities/                  # Core domain entities
│   └── Enums/                     # Enumerations
│
├── CarBookingSystem.Infrastructure/ # Database and external services
│   ├── Migrations/                # EF Core migrations
│   └── Repositories/              # Data repositories
│
└── CarBookingSystemApp/           # MAUI Blazor Hybrid App
    ├── Services/                  # Client-side services
    ├── Components/                # Blazor components
    ├── Pages/                     # Application pages
    └── Program.cs                 # MAUI startup
🚀 Getting Started
Prerequisites
.NET 8 SDK

Visual Studio 2022+ (with MAUI workload)

SQL Server 2019+ or SQL Server Express

Postman or similar API testing tool

Setup Instructions
Clone the Repository

bash
git clone https://github.com/yourusername/CarBookingSystem.git
cd CarBookingSystem
Database Setup

Update connection string in appsettings.json

Run migrations:

bash
cd CarBookingSystem.API
dotnet ef database update
API Configuration

json
{
  "JwtSettings": {
    "SecretKey": "Your_32_Character_Long_Secret_Key_Here",
    "Issuer": "CarBookingSystem",
    "Audience": "CarBookingUsers",
    "ExpiryMinutes": "60"
  }
}
Run the Application

Start the API project

Launch the MAUI app in your preferred platform

🔧 API Endpoints
Authentication
POST /api/auth/register - Register new user

POST /api/auth/login - Login with credentials

POST /api/auth/logout - Logout user

Booking Management
GET /api/bookings - Get all bookings (Admin)

POST /api/bookings - Create new booking (Customer)

GET /api/bookings/{id} - Get booking details

PUT /api/bookings/{id}/status - Update booking status

User Management
GET /api/users - Get all users (Admin)

GET /api/users/{id} - Get user details

PUT /api/users/{id} - Update user profile

🎨 Screenshots
Login Screen	Customer Dashboard	Driver View	Admin Panel
https://screenshots/login.png	https://screenshots/customer.png	https://screenshots/driver.png	https://screenshots/admin.png
📋 User Roles
Customer
Browse available rides

Book a ride with pickup/destination

Track current ride status

View booking history and receipts

Rate drivers

Driver
View pending ride requests

Accept/Reject ride requests

Update ride status

View earnings and trip history

Set availability status

Admin
View all bookings in the system

Manage users and drivers

View system analytics and reports

Monitor system performance

Handle disputes and issues

🔐 Security Features
JWT token authentication

Secure password storage

Role-based access control

HTTPS enforcement

Input validation and sanitization

CSRF protection

SQL injection prevention

📱 Platform Support
✅ Windows

✅ macOS

✅ Android

✅ iOS

✅ Web (via browser)

🚀 Performance Features
Asynchronous API calls

Efficient database queries with EF Core

Optimized for mobile networks

Lazy loading for better UX

Caching strategy for frequently accessed data

🔄 Development Workflow
API Development

bash
cd CarBookingSystem.API
dotnet run
MAUI Development

bash
cd CarBookingSystemApp
dotnet build -t:Run -f net8.0-android
Testing

Unit tests with xUnit

API testing with Swagger

UI testing with bUnit

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.

🤝 Contributing
Fork the repository

Create your feature branch (git checkout -b feature/AmazingFeature)

Commit your changes (git commit -m 'Add some AmazingFeature')

Push to the branch (git push origin feature/AmazingFeature)

Open a Pull Request
