using FluentValidation;
using FluentValidation.AspNetCore.TagHelpers.Examples.Models;

namespace FluentValidation.AspNetCore.TagHelpers.Examples.Validators
{
    public class ProductValidator : AbstractValidator<ProductModel>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.")
                .LessThanOrEqualTo(10000).WithMessage("Price cannot exceed $10,000.");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.")
                .LessThan(10000).WithMessage("Stock must be less than 10,000.");

            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU is required.")
                .Matches(@"^[A-Z]{3}-\d{4}$").WithMessage("SKU must be in format: ABC-1234");

            RuleFor(x => x.ManufacturerEmail)
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .When(x => !string.IsNullOrEmpty(x.ManufacturerEmail));

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
            
            RuleFor(x => x.CreditCard)
                .CreditCard().WithMessage("Please enter a valid credit card number.")
                .When(x => !string.IsNullOrEmpty(x.CreditCard));
        }
    }
}