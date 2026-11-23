namespace Shared.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = "RailwayReservationSystemSuperSecretKey20261123!@#$";
    public string Issuer { get; set; } = "RailwayReservationSystem";
    public string Audience { get; set; } = "RailwayReservationSystemClients";
    public int ExpiryMinutes { get; set; } = 120;
}
