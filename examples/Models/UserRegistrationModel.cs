namespace FluentValidation.AspNetCore.TagHelpers.Examples.Models
{
    public class UserRegistrationModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public int Age { get; set; }
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
    }
}