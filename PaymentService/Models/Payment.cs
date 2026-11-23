using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentId { get; set; }

    [Required]
    public int ReservationId { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentType { get; set; } = "Payment"; // "Payment" or "Refund"

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Success"; // "Success" or "Failed"

    [Required]
    [MaxLength(50)]
    public string TransactionId { get; set; } = string.Empty;
}
