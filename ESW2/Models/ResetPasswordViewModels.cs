namespace ESW2.Models
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string NovaPassword { get; set; }
        public string ConfirmarPassword { get; set; }
    }
}