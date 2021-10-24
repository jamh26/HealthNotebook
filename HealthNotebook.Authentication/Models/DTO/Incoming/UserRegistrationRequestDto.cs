using System.ComponentModel.DataAnnotations;

namespace HealthNotebook.Authentication.Models.DTO.Incoming
{
    public class UserRegistrationRequestDto
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}