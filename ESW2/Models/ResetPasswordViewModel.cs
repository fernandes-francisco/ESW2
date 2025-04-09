

using System.ComponentModel.DataAnnotations;

namespace ESW2.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "O token é obrigatório.")]
        public string Token { get; set; }
        
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string NovaSenha { get; set; }
        
        [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
        [DataType(DataType.Password)]
        [Compare("NovaSenha", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmacaoSenha { get; set; }
    }
}