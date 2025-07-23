using System.ComponentModel.DataAnnotations;

namespace Ideku.Models.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
    }
}