// ResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ESW2.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "O token é obrigatório.")]
        public string Token { get; set; }
        
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        
        [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmPassword { get; set; }
    }
}