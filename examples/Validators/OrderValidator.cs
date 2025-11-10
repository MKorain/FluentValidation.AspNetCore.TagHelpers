using FluentValidation;
using FluentValidation.AspNetCore.TagHelpers.Examples.Models;

namespace FluentValidation.AspNetCore.TagHelpers.Examples.Validators
{
    public class OrderValidator : AbstractValidator<OrderModel>
    {
        public OrderValidator()
        {
            RuleFor(x => x.OrderNumber)
                .NotEmpty().WithMessage("Order number is required.")
                .Length(5, 10).WithMessage("Order number must be between 5 and 10 characters.");
            
            RuleFor(x => x.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than 0.");
            
            RuleFor(x => x.ShippingAddress)
                .SetValidator(new AddressValidator());
            
            RuleFor(x => x.BillingAddress)
                .SetValidator(new AddressValidator());
        }
    }

    public class AddressValidator : AbstractValidator<AddressModel>
    {
        public AddressValidator()
        {
            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(100).WithMessage("Street cannot exceed 100 characters.");
            
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(50).WithMessage("City cannot exceed 50 characters.");
            
            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required.")
                .Length(2).WithMessage("State must be 2 characters (e.g., CA).);
            
            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage("Zip code is required.")
                .Matches("^\\d{5}$").WithMessage("Zip code must be 5 digits.");
            
            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required.");
        }
    }
}