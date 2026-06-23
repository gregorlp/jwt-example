using System.ComponentModel.DataAnnotations;

namespace jwt_example.Model
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
