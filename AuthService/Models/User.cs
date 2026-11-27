//Entity model - db table in C#
using System.ComponentModel.DataAnnotations;//provide attribute (validation,db mapping)
using System.ComponentModel.DataAnnotations.Schema;//control database table behavior

namespace AuthService.Models;

//This class maps to Users table.
[Table("Users")]
public class User
{
    [Key] //PK
    //Database generates value automatically.
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required]  //can't be null
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty; // "Admin" or "User"
}
