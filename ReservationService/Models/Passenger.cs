using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationService.Models;

[Table("Passengers")]
public class Passenger
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PassengerId { get; set; }

    [Required]
    public int ReservationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [Range(1, 120)]
    public int Age { get; set; }

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty; // "Male", "Female", "Other"

    [ForeignKey(nameof(ReservationId))]
    public virtual Reservation? Reservation { get; set; }
}
