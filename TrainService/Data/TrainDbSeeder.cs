using TrainService.Models;

namespace TrainService.Data;

public static class TrainDbSeeder
{
    public static void Seed(TrainDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Trains.Any())
        {
            var baseDate = new DateTime(2026, 12, 1);

            var trains = new List<Train>
            {
                new Train
                {
                    TrainName = "Mumbai Rajdhani",
                    Source = "Mumbai",
                    Destination = "Delhi",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(16, 35, 0),
                    ArrivalTime = new TimeSpan(8, 30, 0),
                    TotalSeats = 120,
                    AvailableSeats = 60,
                    Fare = 1800.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Vande Bharat Express",
                    Source = "Delhi",
                    Destination = "Varanasi",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(6, 0, 0),
                    ArrivalTime = new TimeSpan(14, 0, 0),
                    TotalSeats = 100,
                    AvailableSeats = 80,
                    Fare = 1750.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "August Kranti Rajdhani",
                    Source = "Mumbai",
                    Destination = "Delhi",
                    JourneyDate = baseDate.AddDays(1),
                    DepartureTime = new TimeSpan(17, 10, 0),
                    ArrivalTime = new TimeSpan(9, 50, 0),
                    TotalSeats = 150,
                    AvailableSeats = 150,
                    Fare = 1850.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Chennai Central Mail",
                    Source = "Chennai",
                    Destination = "Bangalore",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(7, 30, 0),
                    ArrivalTime = new TimeSpan(13, 0, 0),
                    TotalSeats = 90,
                    AvailableSeats = 45,
                    Fare = 650.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Kolkata Howrah Express",
                    Source = "Kolkata",
                    Destination = "Patna",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(14, 15, 0),
                    ArrivalTime = new TimeSpan(22, 45, 0),
                    TotalSeats = 110,
                    AvailableSeats = 90,
                    Fare = 850.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Bangalore Rajdhani",
                    Source = "Bangalore",
                    Destination = "Delhi",
                    JourneyDate = baseDate.AddDays(2),
                    DepartureTime = new TimeSpan(20, 0, 0),
                    ArrivalTime = new TimeSpan(5, 55, 0),
                    TotalSeats = 130,
                    AvailableSeats = 110,
                    Fare = 2400.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Shatabdi Express",
                    Source = "Delhi",
                    Destination = "Chandigarh",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(7, 40, 0),
                    ArrivalTime = new TimeSpan(11, 0, 0),
                    TotalSeats = 80,
                    AvailableSeats = 35,
                    Fare = 920.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Konkan Kanya Express",
                    Source = "Mumbai",
                    Destination = "Goa",
                    JourneyDate = baseDate.AddDays(3),
                    DepartureTime = new TimeSpan(23, 05, 0),
                    ArrivalTime = new TimeSpan(10, 45, 0),
                    TotalSeats = 100,
                    AvailableSeats = 75,
                    Fare = 1150.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Deccan Queen",
                    Source = "Pune",
                    Destination = "Mumbai",
                    JourneyDate = baseDate,
                    DepartureTime = new TimeSpan(7, 15, 0),
                    ArrivalTime = new TimeSpan(10, 25, 0),
                    TotalSeats = 75,
                    AvailableSeats = 50,
                    Fare = 450.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Tejas Express",
                    Source = "Ahmedabad",
                    Destination = "Mumbai",
                    JourneyDate = baseDate.AddDays(4),
                    DepartureTime = new TimeSpan(6, 40, 0),
                    ArrivalTime = new TimeSpan(13, 10, 0),
                    TotalSeats = 95,
                    AvailableSeats = 80,
                    Fare = 1450.00m,
                    Status = "Active"
                },
                new Train
                {
                    TrainName = "Garib Rath Express",
                    Source = "Delhi",
                    Destination = "Mumbai",
                    JourneyDate = baseDate.AddDays(5),
                    DepartureTime = new TimeSpan(15, 55, 0),
                    ArrivalTime = new TimeSpan(8, 15, 0),
                    TotalSeats = 140,
                    AvailableSeats = 140,
                    Fare = 1050.00m,
                    Status = "Active"
                }
            };

            context.Trains.AddRange(trains);
            context.SaveChanges();
        }
    }
}
