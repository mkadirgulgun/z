namespace z.Models
{
    public class Login
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string Password { get; set; }
        public string PasswordRepeat { get; set; }
		public string Email { get; set; }
        public string? Gender { get; set; }
        public int? IsApproved { get; set; }
        public string? ValidKey { get; set; }
        public string? ResetKey { get; set; }
    }   
    public class LoginRequestModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class ChangePassword
    {
        public  int Id { get; set; }    
        public string OldPassword { get; set; } 
        public string Password { get; set; }
        public string PasswordRepeat { get; set; }
    }
    public class ForgotPassword
    {
        public int Id { get; set; }
        public string Password { get; set; }
        public string PasswordRepeat { get; set; }
    }
}
