using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationService.Models;

[Table("Cancellations")]
public class Cancellation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CancellationId { get; set; }

    [Required]
    public int ReservationId { get; set; }

    [Required]
    public DateTime CancellationDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal RefundAmount { get; set; }

    [ForeignKey(nameof(ReservationId))]
    public virtual Reservation? Reservation { get; set; }
}
