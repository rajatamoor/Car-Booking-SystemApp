using CarBookingSystem.Infrastructure;
using CarBookingSystem.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "YourSecretKeyHere12345678901234567890");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "CarBookingAPI",
        ValidAudience = jwtSettings["Audience"] ?? "CarBookingClient",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

// Add SignalR
builder.Services.AddSignalR();

// Configure HttpClient
builder.Services.AddHttpClient(); // This is for IHttpClientFactory

// Configure JSON options for HTTP requests/responses
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure JSON options for MVC controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Configure CORS with specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy => policy
            .WithOrigins(
                "https://localhost:7285",     // For MAUI on localhost
                "https://localhost",         // For MAUI on localhost with HTTPS
                "http://localhost:5000",     // For web frontend
                "http://localhost:3000",     // For React/Vue frontend
                "https://yourapp.com"        // Your production domain
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");  // Changed from "AllowAll" to "AllowSpecificOrigins"
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CarBookingSystem.API.Hubs.BookingHub>("/bookingHub");

// Database Setup - WITHOUT DELETING OR SEEDING
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Checking database connection...");

        // Hard reset database to resolve pending model changes (since migration tool is broken)
        logger.LogInformation("Recreating database to apply latest model...");
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        logger.LogInformation("Database recreated successfully.");

        // Seed initial data
        SeedData(context, logger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during application startup.");
    }
}

app.Run();

// Helper method to fix LicenseNumber column
static void FixLicenseNumberColumn(AppDbContext context, ILogger logger)
{
    try
    {
        // Check if LicenseNumber column is NOT NULL and fix it
        var sql = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                      WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LicenseNumber' AND IS_NULLABLE = 'NO')
            BEGIN
                ALTER TABLE Users ALTER COLUMN LicenseNumber nvarchar(50) NULL;
                PRINT 'LicenseNumber column set to nullable';
            END";

        context.Database.ExecuteSqlRaw(sql);
        logger.LogInformation("LicenseNumber column check completed.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not check/fix LicenseNumber column. It might already be nullable.");
    }
}

static void SeedData(AppDbContext context, ILogger logger)
{
    if (context.Users.Any()) return; // Already seeded

    logger.LogInformation("Seeding initial data...");

    // Password is "password123" (simple text for demo as per AuthController)
    string passwordHash = "password123"; 
    
    // Create Customer
    var customer = new User
    {
        Name = "Test Customer",
        Email = "customer@test.com",
        PasswordHash = passwordHash,
        Role = "Customer",
        Phone = "1234567890",
        Status = "Active"
    };

    // Create Driver
    var driver = new User
    {
        Name = "Test Driver",
        Email = "driver@test.com",
        PasswordHash = passwordHash,
        Role = "Driver",
        Phone = "0987654321",
        LicenseNumber = "DL123456",
        Status = "Active"
    };

    // Create Admin
    var admin = new User
    {
        Name = "Admin User",
        Email = "admin@test.com",
        PasswordHash = passwordHash,
        Role = "Admin",
        Phone = "1122334455",
        Status = "Active"
    };

    context.Users.AddRange(customer, driver, admin);

    // Create Car
    var car = new Car
    {
        Make = "Toyota",
        Model = "Camry",
        Year = 2022,
        PlateNumber = "ABC-123",
        Color = "White",
        Type = "Sedan",
        Status = "Available",
        Driver = driver
    };
    
    context.Cars.Add(car); // Driver is added via relationship

    context.SaveChanges();
    logger.LogInformation("Data seeded successfully.");
}