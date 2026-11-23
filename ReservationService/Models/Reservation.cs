using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationService.Models;

[Table("Reservations")]
public class Reservation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReservationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PNR { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [Required]
    public int TrainId { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime JourneyDate { get; set; }

    [Required]
    public int NoOfPassengers { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalFare { get; set; }

    [Required]
    [MaxLength(20)]
    public string BookingStatus { get; set; } = "Pending"; // "Pending", "Confirmed", "Cancelled", "Failed"

    [Required]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();

    public virtual Cancellation? Cancellation { get; set; }
}
