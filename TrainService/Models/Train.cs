using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainService.Models;

[Table("Trains")]
public class Train
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TrainId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TrainName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "date")]
    public DateTime JourneyDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public TimeSpan DepartureTime { get; set; }

    [Required]
    public TimeSpan ArrivalTime { get; set; }

    [Required]
    public int TotalSeats { get; set; }

    [Required]
    public int AvailableSeats { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Fare { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // "Active" or "Inactive"

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
