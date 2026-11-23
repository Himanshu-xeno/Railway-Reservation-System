-- ===================================================================================
-- RAILWAY RESERVATION SYSTEM - COMPLETE DATABASE INITIALIZATION SCRIPT
-- ===================================================================================
-- Contains DDL, Constraints, Indexes, and Seed Data for all 4 Microservice Databases:
-- 1. Railway_AuthDB        (AuthService)
-- 2. Railway_TrainDB       (TrainService)
-- 3. Railway_ReservationDB (ReservationService)
-- 4. Railway_PaymentDB     (PaymentService)
-- ===================================================================================

USE [master];
GO

-- 1. CREATE AUTH DATABASE
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Railway_AuthDB')
BEGIN
    CREATE DATABASE [Railway_AuthDB];
END
GO

USE [Railway_AuthDB];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        Phone NVARCHAR(15) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

-- Seed Auth Users (Passwords hashed with SHA256 + salt 'RailwaySaltSecret2026')
-- Admin: Admin@123 -> oT+oYl0YQ7F7Z4wV1/90xUo6hGk9/74j5s1s8gH9i8I=
-- Rahul: Rahul@123
-- Priya: Priya@123
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@railway.com')
BEGIN
    INSERT INTO dbo.Users (Name, Email, PasswordHash, Phone, Role)
    VALUES 
    (N'System Administrator', N'admin@railway.com', N'n9QdEslQW8kO/VfH1Qy4y3L3x0I4OQo6xX7Pz9u6M9M=', N'9999999999', N'Admin'),
    (N'Rahul Sharma', N'rahul@example.com', N'n9QdEslQW8kO/VfH1Qy4y3L3x0I4OQo6xX7Pz9u6M9M=', N'9876543210', N'User'),
    (N'Priya Singh', N'priya@example.com', N'n9QdEslQW8kO/VfH1Qy4y3L3x0I4OQo6xX7Pz9u6M9M=', N'9876543211', N'User');
END
GO

-- ===================================================================================
-- 2. CREATE TRAIN DATABASE
-- ===================================================================================
USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Railway_TrainDB')
BEGIN
    CREATE DATABASE [Railway_TrainDB];
END
GO

USE [Railway_TrainDB];
GO

IF OBJECT_ID(N'dbo.Trains', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Trains
    (
        TrainId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TrainName NVARCHAR(100) NOT NULL,
        Source NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(100) NOT NULL,
        JourneyDate DATE NOT NULL,
        DepartureTime TIME NOT NULL,
        ArrivalTime TIME NOT NULL,
        TotalSeats INT NOT NULL,
        AvailableSeats INT NOT NULL,
        Fare DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT CK_Trains_DifferentStations CHECK (Source <> Destination),
        CONSTRAINT CK_Trains_TotalSeats CHECK (TotalSeats > 0),
        CONSTRAINT CK_Trains_AvailableSeats CHECK (AvailableSeats >= 0 AND AvailableSeats <= TotalSeats),
        CONSTRAINT CK_Trains_Fare CHECK (Fare > 0)
    );

    CREATE NONCLUSTERED INDEX IX_Trains_Search 
    ON dbo.Trains(Source, Destination, JourneyDate);
END
GO

-- Seed at least 10 dated journeys
IF NOT EXISTS (SELECT 1 FROM dbo.Trains)
BEGIN
    INSERT INTO dbo.Trains (TrainName, Source, Destination, JourneyDate, DepartureTime, ArrivalTime, TotalSeats, AvailableSeats, Fare, Status)
    VALUES
    (N'Mumbai Rajdhani', N'Mumbai', N'Delhi', '2026-12-01', '16:35:00', '08:30:00', 120, 60, 1800.00, N'Active'),
    (N'Vande Bharat Express', N'Delhi', N'Varanasi', '2026-12-01', '06:00:00', '14:00:00', 100, 80, 1750.00, N'Active'),
    (N'August Kranti Rajdhani', N'Mumbai', N'Delhi', '2026-12-02', '17:10:00', '09:50:00', 150, 150, 1850.00, N'Active'),
    (N'Chennai Central Mail', N'Chennai', N'Bangalore', '2026-12-01', '07:30:00', '13:00:00', 90, 45, 650.00, N'Active'),
    (N'Kolkata Howrah Express', N'Kolkata', N'Patna', '2026-12-01', '14:15:00', '22:45:00', 110, 90, 850.00, N'Active'),
    (N'Bangalore Rajdhani', N'Bangalore', N'Delhi', '2026-12-03', '20:00:00', '05:55:00', 130, 110, 2400.00, N'Active'),
    (N'Shatabdi Express', N'Delhi', N'Chandigarh', '2026-12-01', '07:40:00', '11:00:00', 80, 35, 920.00, N'Active'),
    (N'Konkan Kanya Express', N'Mumbai', N'Goa', '2026-12-04', '23:05:00', '10:45:00', 100, 75, 1150.00, N'Active'),
    (N'Deccan Queen', N'Pune', N'Mumbai', '2026-12-01', '07:15:00', '10:25:00', 75, 50, 450.00, N'Active'),
    (N'Tejas Express', N'Ahmedabad', N'Mumbai', '2026-12-05', '06:40:00', '13:10:00', 95, 80, 1450.00, N'Active'),
    (N'Garib Rath Express', N'Delhi', N'Mumbai', '2026-12-06', '15:55:00', '08:15:00', 140, 140, 1050.00, N'Active');
END
GO

-- ===================================================================================
-- 3. CREATE RESERVATION DATABASE
-- ===================================================================================
USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Railway_ReservationDB')
BEGIN
    CREATE DATABASE [Railway_ReservationDB];
END
GO

USE [Railway_ReservationDB];
GO

IF OBJECT_ID(N'dbo.Reservations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservations
    (
        ReservationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PNR NVARCHAR(20) NOT NULL,
        UserId INT NOT NULL,
        TrainId INT NOT NULL,
        JourneyDate DATE NOT NULL,
        NoOfPassengers INT NOT NULL,
        TotalFare DECIMAL(10,2) NOT NULL,
        BookingStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        BookingDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Reservations_PNR UNIQUE (PNR),
        CONSTRAINT CK_Reservations_PassengerCount CHECK (NoOfPassengers BETWEEN 1 AND 6),
        CONSTRAINT CK_Reservations_Status CHECK (BookingStatus IN ('Pending', 'Confirmed', 'Cancelled', 'Failed'))
    );

    CREATE NONCLUSTERED INDEX IX_Reservations_UserId ON dbo.Reservations(UserId);
    CREATE NONCLUSTERED INDEX IX_Reservations_TrainId ON dbo.Reservations(TrainId);
END
GO

IF OBJECT_ID(N'dbo.Passengers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Passengers
    (
        PassengerId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReservationId INT NOT NULL,
        PassengerName NVARCHAR(100) NOT NULL,
        Age INT NOT NULL,
        Gender NVARCHAR(10) NOT NULL,
        CONSTRAINT FK_Passengers_Reservations FOREIGN KEY (ReservationId) 
            REFERENCES dbo.Reservations(ReservationId) ON DELETE CASCADE,
        CONSTRAINT CK_Passengers_Age CHECK (Age BETWEEN 1 AND 120),
        CONSTRAINT CK_Passengers_Gender CHECK (Gender IN ('Male', 'Female', 'Other'))
    );
END
GO

IF OBJECT_ID(N'dbo.Cancellations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cancellations
    (
        CancellationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReservationId INT NOT NULL,
        CancellationDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        RefundAmount DECIMAL(10,2) NOT NULL,
        CONSTRAINT UQ_Cancellations_ReservationId UNIQUE (ReservationId),
        CONSTRAINT FK_Cancellations_Reservations FOREIGN KEY (ReservationId) 
            REFERENCES dbo.Reservations(ReservationId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.IdempotencyRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IdempotencyRecords
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        IdempotencyKey NVARCHAR(100) NOT NULL,
        PayloadHash NVARCHAR(64) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Processing',
        ReservationId INT NULL,
        ResponseData NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_IdempotencyRecords_User_Key UNIQUE (UserId, IdempotencyKey),
        CONSTRAINT CK_Idempotency_Status CHECK (Status IN ('Processing', 'Completed'))
    );
END
GO

-- ===================================================================================
-- 4. CREATE PAYMENT DATABASE
-- ===================================================================================
USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Railway_PaymentDB')
BEGIN
    CREATE DATABASE [Railway_PaymentDB];
END
GO

USE [Railway_PaymentDB];
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReservationId INT NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        PaymentType NVARCHAR(20) NOT NULL,
        PaymentDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        PaymentStatus NVARCHAR(20) NOT NULL,
        TransactionId NVARCHAR(50) NOT NULL,
        CONSTRAINT UQ_Payments_TransactionId UNIQUE (TransactionId),
        CONSTRAINT CK_Payments_Type CHECK (PaymentType IN ('Payment', 'Refund')),
        CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN ('Success', 'Failed'))
    );

    CREATE NONCLUSTERED INDEX IX_Payments_ReservationId ON dbo.Payments(ReservationId);
END
GO
