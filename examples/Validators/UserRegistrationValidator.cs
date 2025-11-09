using FluentValidation;
using FluentValidation.AspNetCore.TagHelpers.Examples.Models;

namespace FluentValidation.AspNetCore.TagHelpers.Examples.Validators
{
    public class UserRegistrationValidator : AbstractValidator<UserRegistrationModel>
    {
        public UserRegistrationValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 20).WithMessage("Username must be between 3 and 20 characters.");
            
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Please enter a valid email address.");
            
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
            
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords must match.");
            
            RuleFor(x => x.Age)
                .InclusiveBetween(18, 120).WithMessage("Age must be between 18 and 120.");
            
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d{10}$").WithMessage("Phone number must be 10 digits.");
            
            RuleFor(x => x.Website)
                .Matches(@"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$")
                .When(x => !string.IsNullOrEmpty(x.Website))
                .WithMessage("Please enter a valid URL.");
        }
    }
}