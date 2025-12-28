🚗 Car Booking System
Project Overview

The Car Booking System is a modern cross-platform application built using ASP.NET Core Web API and .NET MAUI Blazor Hybrid. It provides a complete ride-booking solution with role-based access for Customers, Drivers, and Administrators, designed using clean architecture and scalable backend practices.

✨ Key Features
👥 Multi-Role Management

Customers can book rides, track ride status, and view booking history

Drivers can accept or reject rides, update ride status, and manage trips

Admins can manage users, drivers, and view all bookings

🔐 Authentication & Security

JWT-based authentication

Role-based authorization

Secure password handling

MAUI Secure Storage for token management

🚗 Booking & Ride Management

Ride booking with pickup and drop-off

Status tracking (Pending, Accepted, In Progress, Completed)

Driver assignment system

Booking history access

📱 Cross-Platform Support

.NET MAUI Blazor Hybrid application

Runs on Windows, Android, iOS, macOS

Responsive UI using Blazor & Bootstrap 5

🛠 Technology Stack

Backend

ASP.NET Core 8 Web API

Entity Framework Core

SQL Server

JWT Authentication

Swagger / OpenAPI

Frontend

.NET MAUI Blazor Hybrid

Blazor Components

Bootstrap 5

Architecture

Clean Architecture

Dependency Injection

Async/Await programming

📁 Project Structure

CarBookingSystem.API – Web API layer

CarBookingSystem.Domain – Core entities & DTOs

CarBookingSystem.Infrastructure – Database & migrations

CarBookingSystemApp – MAUI Blazor Hybrid client

🚀 Getting Started

Clone the repository

Configure the database connection

Apply EF Core migrations

Run the API and MAUI application
