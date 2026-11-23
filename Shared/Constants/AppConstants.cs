namespace Shared.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class BookingStatusConstants
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
}

public static class PaymentStatusConstants
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}

public static class PaymentTypeConstants
{
    public const string Payment = "Payment";
    public const string Refund = "Refund";
}

public static class TrainStatusConstants
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}
