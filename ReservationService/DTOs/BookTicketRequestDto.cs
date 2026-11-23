using System.ComponentModel.DataAnnotations;

namespace ReservationService.DTOs;

public class BookTicketRequestDto
{
    [Required(ErrorMessage = "Train ID is required")]
    public int TrainId { get; set; }

    [Required(ErrorMessage = "At least one passenger is required")]
    [MinLength(1, ErrorMessage = "At least one passenger is required")]
    [MaxLength(6, ErrorMessage = "A maximum of 6 passengers can be booked per reservation")]
    public List<PassengerDto> Passengers { get; set; } = new();
}
