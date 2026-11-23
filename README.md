# Railway Reservation System - Microservices Solution

A complete, beginner-friendly, and production-quality Railway Reservation System built with **ASP.NET Core 10 Web API**, **Entity Framework Core**, **SQL Server**, **JWT Authentication**, and **YARP API Gateway**.

This project implements all business logic, data models, workflows, idempotency rules, overbooking prevention, and sequence flows strictly following the Low-Level Design (LLD) specifications.

---

## 1. Solution Architecture & Folder Structure

The solution strictly adheres to the layered flow:
$$\text{Controller} \longrightarrow \text{Service} \longrightarrow \text{Repository} \longrightarrow \text{DbContext} \longrightarrow \text{SQL Server}$$

- **Controllers**: Handle HTTP requests, DTO mapping, and response codes.
- **Services**: Contain all domain business rules, cross-service orchestrations, and validations.
- **Repositories**: Execute database queries using EF Core with zero business logic.
- **Middleware**: Centralized error handling returning uniform JSON responses: `{ statusCode, message, timestamp }`.

```
RailwayReservationSystem
│
├── RailwayReservationSystem.sln     # Visual Studio 2022 Solution file
├── RailwayReservationSystem.slnx    # Modern .NET Solution format
├── DatabaseSetup.sql                # Complete SQL script for all 4 databases
│
├── ApiGateway/                      # YARP Reverse Proxy & JWT validation (Port 7000)
│   ├── appsettings.json
│   ├── Program.cs
│   └── Properties/launchSettings.json
│
├── AuthService/                     # User Registration, Login & JWT (Port 7001)
│   ├── Controllers/AuthController.cs
│   ├── Data/AuthDbContext.cs, AuthDbSeeder.cs
│   ├── DTOs/RegisterRequestDto.cs, LoginRequestDto.cs, UserDto.cs, etc.
│   ├── Models/User.cs
│   ├── Repositories/IUserRepository.cs, UserRepository.cs
│   ├── Services/IAuthService.cs, AuthService.cs
│   ├── Middleware/GlobalExceptionMiddleware.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── TrainService/                    # Dated Train Journeys, Search & Seats (Port 7002)
│   ├── Controllers/TrainController.cs
│   ├── Data/TrainDbContext.cs, TrainDbSeeder.cs
│   ├── DTOs/CreateTrainRequestDto.cs, TrainResponseDto.cs, ReserveSeatsDto.cs, etc.
│   ├── Models/Train.cs
│   ├── Repositories/ITrainRepository.cs, TrainRepository.cs
│   ├── Services/ITrainService.cs, TrainService.cs
│   ├── Middleware/GlobalExceptionMiddleware.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── ReservationService/              # Bookings, PNR, Idempotency & Refunds (Port 7003)
│   ├── Controllers/ReservationController.cs
│   ├── Data/ReservationDbContext.cs
│   ├── DTOs/BookTicketRequestDto.cs, BookingResponseDto.cs, FareEstimateDto.cs, etc.
│   ├── Models/Reservation.cs, Passenger.cs, Cancellation.cs, IdempotencyRecord.cs
│   ├── Repositories/IReservationRepository.cs, IIdempotencyRepository.cs, etc.
│   ├── Services/IReservationService.cs, ITrainClient.cs, IPaymentClient.cs, etc.
│   ├── Middleware/GlobalExceptionMiddleware.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── PaymentService/                  # Simulated Payments & Refund Logs (Port 7004)
│   ├── Controllers/PaymentController.cs
│   ├── Data/PaymentDbContext.cs
│   ├── DTOs/ProcessPaymentRequestDto.cs, PaymentResponseDto.cs, etc.
│   ├── Models/Payment.cs
│   ├── Repositories/IPaymentRepository.cs, PaymentRepository.cs
│   ├── Services/IPaymentService.cs, PaymentService.cs
│   ├── Middleware/GlobalExceptionMiddleware.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── Shared/                          # Shared Constants, Exceptions, and Settings
│   ├── Common/ErrorResponse.cs
│   ├── Constants/AppConstants.cs
│   ├── Exceptions/AppExceptions.cs
│   └── Settings/JwtSettings.cs
│
└── Tests/                           # NUnit + Moq Unit Tests (34 test cases)
    ├── Auth/AuthServiceTests.cs
    ├── Train/TrainServiceTests.cs
    ├── Reservation/ReservationServiceTests.cs
    └── Payment/PaymentServiceTests.cs
```

---

## 2. Port Allocation & Endpoints Summary

| Service | Port | Swagger URL | Responsibilities |
| :--- | :---: | :--- | :--- |
| **ApiGateway** | `7000` | `http://localhost:7000/swagger` | Central entry point, YARP reverse proxy, JWT forwarding |
| **AuthService** | `7001` | `http://localhost:7001/swagger` | User registration, login, token generation, user details |
| **TrainService** | `7002` | `http://localhost:7002/swagger` | Train journey management, search, seat reserve/release |
| **ReservationService** | `7003` | `http://localhost:7003/swagger` | Fare estimate, ticket booking, PNR lookup, cancellations |
| **PaymentService** | `7004` | `http://localhost:7004/swagger` | Simulated payments, refund processing, transaction audit |

---

## 3. Database Architecture (Database-per-Service)

The system maintains 4 independent databases across 7 tables:

1. **`Railway_AuthDB`**
   - `Users`: `UserId` (PK), `Name`, `Email` (UQ), `PasswordHash`, `Phone`, `Role` ("Admin" / "User").
   - **Seed**: `admin@railway.com` / `Admin@123` (Admin), sample users `rahul@example.com`, `priya@example.com`.

2. **`Railway_TrainDB`**
   - `Trains`: `TrainId` (PK), `TrainName`, `Source`, `Destination`, `JourneyDate`, `DepartureTime`, `ArrivalTime`, `TotalSeats`, `AvailableSeats`, `Fare`, `Status` ("Active"/"Inactive"), `RowVersion` (concurrency token).
   - **Seed**: 11 realistic train journeys (Mumbai Rajdhani, Vande Bharat Express, Shatabdi Express, etc.).

3. **`Railway_ReservationDB`**
   - `Reservations`: `ReservationId` (PK), `PNR` (UQ), `UserId` (logical ref), `TrainId` (logical ref), `JourneyDate`, `NoOfPassengers` (1-6), `TotalFare`, `BookingStatus` ("Pending", "Confirmed", "Cancelled", "Failed"), `BookingDate`.
   - `Passengers`: `PassengerId` (PK), `ReservationId` (FK), `PassengerName`, `Age`, `Gender`.
   - `Cancellations`: `CancellationId` (PK), `ReservationId` (FK, UQ), `CancellationDate`, `RefundAmount`.
   - `IdempotencyRecords`: `Id` (PK), `UserId`, `IdempotencyKey` (UQ with UserId), `PayloadHash`, `Status` ("Processing", "Completed"), `ReservationId`, `CreatedAt`.

4. **`Railway_PaymentDB`**
   - `Payments`: `PaymentId` (PK), `ReservationId` (logical ref), `Amount`, `PaymentType` ("Payment", "Refund"), `PaymentDate`, `PaymentStatus` ("Success", "Failed"), `TransactionId` (UQ).

---

## 4. How to Set Up & Run the Project

### Option A: Using Visual Studio 2022
1. Open `RailwayReservationSystem.sln`.
2. Right click on the Solution in **Solution Explorer** $\rightarrow$ **Configure Startup Projects...**
3. Select **Multiple startup projects**:
   - `ApiGateway` $\rightarrow$ Start
   - `AuthService` $\rightarrow$ Start
   - `TrainService` $\rightarrow$ Start
   - `ReservationService` $\rightarrow$ Start
   - `PaymentService` $\rightarrow$ Start
4. Press **F5** or click **Start**.
5. All Swagger UI windows will launch automatically!

### Option B: Running from Command Line (PowerShell)
You can launch each service in its own terminal window:
```powershell
# Terminal 1 - Auth Service
dotnet run --project AuthService/AuthService.csproj

# Terminal 2 - Train Service
dotnet run --project TrainService/TrainService.csproj

# Terminal 3 - Payment Service
dotnet run --project PaymentService/PaymentService.csproj

# Terminal 4 - Reservation Service
dotnet run --project ReservationService/ReservationService.csproj

# Terminal 5 - API Gateway
dotnet run --project ApiGateway/ApiGateway.csproj
```

### Option C: Database Setup
Every service auto-creates and seeds its database on first startup using `context.Database.EnsureCreated()` and custom seeders.

If you prefer to set up SQL Server manually, simply run `DatabaseSetup.sql` in SQL Server Management Studio (SSMS) or Azure Data Studio.

#### EF Core Migration Commands (if adding new migrations):
```powershell
# AuthService
dotnet ef migrations add InitialCreate --project AuthService --startup-project AuthService
dotnet ef database update --project AuthService --startup-project AuthService

# TrainService
dotnet ef migrations add InitialCreate --project TrainService --startup-project TrainService
dotnet ef database update --project TrainService --startup-project TrainService

# ReservationService
dotnet ef migrations add InitialCreate --project ReservationService --startup-project ReservationService
dotnet ef database update --project ReservationService --startup-project ReservationService

# PaymentService
dotnet ef migrations add InitialCreate --project PaymentService --startup-project PaymentService
dotnet ef database update --project PaymentService --startup-project PaymentService
```

---

## 5. End-to-End API Flow Walkthrough

### Step 1: User Login & Token Retrieval
- **URL**: `POST http://localhost:7001/api/auth/login` (or via Gateway `http://localhost:7000/auth/login`)
- **Request Body**:
```json
{
  "email": "rahul@example.com",
  "password": "Rahul@123"
}
```
- **Response** (HTTP 200):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 2,
  "role": "User"
}
```
*Copy the returned token and click **Authorize** in Swagger UI, entering `Bearer <token>`.*

---

### Step 2: Search for Available Trains
- **URL**: `GET http://localhost:7000/trains/search?source=Mumbai&destination=Delhi&journeyDate=2026-12-01`
- **Response** (HTTP 200):
```json
[
  {
    "trainId": 1,
    "trainName": "Mumbai Rajdhani",
    "source": "Mumbai",
    "destination": "Delhi",
    "journeyDate": "2026-12-01T00:00:00",
    "departureTime": "16:35:00",
    "arrivalTime": "08:30:00",
    "totalSeats": 120,
    "availableSeats": 60,
    "fare": 1800.00,
    "status": "Active"
  }
]
```

---

### Step 3: Estimate Fare
- **URL**: `POST http://localhost:7000/reservations/fare-estimate`
- **Request Body**:
```json
{
  "trainId": 1,
  "passengerCount": 2
}
```
- **Response** (HTTP 200):
```json
{
  "trainId": 1,
  "trainName": "Mumbai Rajdhani",
  "passengerCount": 2,
  "farePerPassenger": 1800.00,
  "totalFare": 3600.00
}
```

---

### Step 4: Book Ticket (with Idempotency)
- **URL**: `POST http://localhost:7000/reservations`
- **Headers**:
  - `Authorization: Bearer <token>`
  - `Idempotency-Key: 7c31a2b0-7a23-4d5a-a44c-123456789abc`
- **Request Body**:
```json
{
  "trainId": 1,
  "passengers": [
    { "passengerName": "Rahul Sharma", "age": 24, "gender": "Male" },
    { "passengerName": "Amit Sharma", "age": 28, "gender": "Male" }
  ]
}
```
- **Response** (HTTP 201 Created):
```json
{
  "reservationId": 1,
  "pnr": "PNR2026110001",
  "userId": 2,
  "trainId": 1,
  "journeyDate": "2026-12-01T00:00:00",
  "noOfPassengers": 2,
  "totalFare": 3600.00,
  "bookingStatus": "Confirmed",
  "bookingDate": "2026-11-23T11:45:00Z",
  "passengers": [
    { "passengerName": "Rahul Sharma", "age": 24, "gender": "Male" },
    { "passengerName": "Amit Sharma", "age": 28, "gender": "Male" }
  ]
}
```
*Note: Resending the exact same request with the same `Idempotency-Key` returns the stored result with no duplicate booking or seat deduction. Sending a different request payload with the same key returns HTTP 409 Conflict.*

---

### Step 5: Cancel Booking & Receive 90% Refund
- **URL**: `POST http://localhost:7000/reservations/1/cancel`
- **Headers**: `Authorization: Bearer <token>`
- **Response** (HTTP 200 OK):
```json
{
  "message": "Booking cancelled successfully. Refund is being processed.",
  "pnr": "PNR2026110001",
  "status": "Cancelled",
  "refundAmount": 3240.00
}
```
- Total Fare: 3600.00
- Cancellation Charge (10%): 360.00
- Refund Amount (90%): 3240.00
- Held seats are automatically released back to `TrainService`.

---

## 6. Unit Testing Suite

All 34 test cases specified in LLD Section 17.2 have been fully implemented with **NUnit + Moq** and pass with 100% success:

```powershell
dotnet test Tests/Tests.csproj
```
**Output**:
```
Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 2 s - Tests.dll (net10.0)
```

### Coverage by Service:
- **AuthServiceTests** (TC-01 to TC-07): Valid registration, duplicate email rejection (409), password hashing verification, login credentials validation, non-existent user handling, and JWT claims integrity.
- **TrainServiceTests** (TC-08 to TC-16): Train creation, same source/destination rejection (400), past date validation (400), train search with matches/no-matches, seat availability lookup, overbooking prevention (409), atomic seat reservation, and seat release logic.
- **ReservationServiceTests** (TC-17 to TC-30): Booking with 1-6 passengers, compensating seat release on payment failure, valid/invalid PNR searches, owner authorization check (403), duplicate/in-progress idempotency checks (409), confirmed booking cancellation, and 90% refund computation.
- **PaymentServiceTests** (TC-31 to TC-33): Process simulated payment, duplicate payment protection (409), zero amount rejection (400), unique TransactionId generation (`TXN...` and `REF...`), and refund validation.
