using System.ComponentModel.DataAnnotations;

namespace HealthNotebook.Authentication.Models.DTO.Generic
{
    public class TokenDataDto
    {
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
    }
}