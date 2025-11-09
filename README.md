# FluentValidation.AspNetCore.TagHelpers

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A powerful ASP.NET Core 9 MVC Tag Helper that seamlessly integrates FluentValidation with jQuery Unobtrusive Validation, automatically generating client-side validation attributes from your FluentValidation rules.

## Features

✅ **Automatic Client-Side Validation** - Converts FluentValidation rules to jQuery Unobtrusive Validation attributes  
✅ **Comprehensive Validator Support** - Supports all common FluentValidation validators  
✅ **Nested Complex Types** - Handles nested objects and complex type hierarchies  
✅ **Performance Optimized** - Built-in caching for validators and descriptors  
✅ **Custom Error Messages** - Automatically includes your custom validation messages  
✅ **Easy Integration** - Simply add `asp-fluent-model` to your forms  
✅ **Zero Configuration** - Works out of the box with ASP.NET Core DI  

## Supported Validators

| FluentValidation Validator | jQuery Validation Attribute | Description |
|----------------------------|----------------------------|-------------|
| `NotEmpty` / `NotNull` | `data-val-required` | Required field validation |
| `EmailAddress` | `data-val-email` | Email format validation |
| `Length` | `data-val-length` | Min/max length validation |
| `MinimumLength` | `data-val-minlength` | Minimum length validation |
| `MaximumLength` | `data-val-maxlength` | Maximum length validation |
| `InclusiveBetween` / `ExclusiveBetween` | `data-val-range` | Numeric range validation |
| `GreaterThan` / `GreaterThanOrEqual` | `data-val-min` | Minimum value validation |
| `LessThan` / `LessThanOrEqual` | `data-val-max` | Maximum value validation |
| `Matches` (Regex) | `data-val-regex` | Pattern matching validation |
| `CreditCard` | `data-val-creditcard` | Credit card format validation |
| Custom validators | `data-val-custom` | Custom validation rules |

## Installation

### Step 1: Install NuGet Packages

```bash
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### Step 2: Add the Tag Helper to Your Project

Copy the `FluentFormTagHelper.cs` file from the `src` directory into your ASP.NET Core project.

### Step 3: Register FluentValidation in Program.cs

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Register all validators from the assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### Step 4: Import Tag Helper in _ViewImports.cshtml

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, YourNamespace
```

## Quick Start

### 1. Create Your Model

```csharp
public class UserRegistrationModel
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Age { get; set; }
}
```

### 2. Create Your Validator

```csharp
using FluentValidation;

public class UserRegistrationValidator : AbstractValidator<UserRegistrationModel>
{
    public UserRegistrationValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 20).WithMessage("Username must be between 3 and 20 characters.");
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Please enter a valid email address.");
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
        
        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120).WithMessage("Age must be between 18 and 120.");
    }
}
```

### 3. Create Your View with the Tag Helper

```cshtml
@model UserRegistrationModel

<form asp-fluent-model="@Model" asp-action="Register" method="post">
    <div class="form-group">
        <label asp-for="Username"></label>
        <input asp-for="Username" class="form-control" />
        <span asp-validation-for="Username" class="text-danger"></span>
    </div>
    
    <div class="form-group">
        <label asp-for="Email"></label>
        <input asp-for="Email" class="form-control" type="email" />
        <span asp-validation-for="Email" class="text-danger"></span>
    </div>
    
    <div class="form-group">
        <label asp-for="Password"></label>
        <input asp-for="Password" class="form-control" type="password" />
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>
    
    <div class="form-group">
        <label asp-for="Age"></label>
        <input asp-for="Age" class="form-control" type="number" />
        <span asp-validation-for="Age" class="text-danger"></span>
    </div>
    
    <button type="submit" class="btn btn-primary">Register</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

That's it! The Tag Helper will automatically generate all the necessary `data-val-*` attributes for client-side validation.

## How It Works

1. **FluentFormTagHelper** targets `<form>` elements with `asp-fluent-model` attribute
2. Resolves the model's `IValidator<T>` from the DI container
3. Extracts all validation rules using FluentValidation's `CreateDescriptor()`
4. Stores the rules in `ViewData` for input elements to access
5. **FluentValidationInputTagHelper** applies to all `<input>`, `<select>`, and `<textarea>` elements with `asp-for`
6. Reads the stored validation rules and generates appropriate `data-val-*` attributes
7. jQuery Unobtrusive Validation uses these attributes for client-side validation

## Advanced Features

### Nested Complex Types

The Tag Helper automatically handles nested objects:

```csharp
public class OrderModel
{
    public string OrderNumber { get; set; }
    public AddressModel ShippingAddress { get; set; }
}

public class AddressModel
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}
```

```cshtml
<form asp-fluent-model="@Model" asp-action="CreateOrder">
    <input asp-for="OrderNumber" class="form-control" />
    <input asp-for="ShippingAddress.Street" class="form-control" />
    <input asp-for="ShippingAddress.City" class="form-control" />
    <input asp-for="ShippingAddress.ZipCode" class="form-control" />
</form>
```

### Performance Optimization

The Tag Helper includes built-in caching mechanisms:
- Validators are cached per model type
- Descriptors are cached per validator type
- Rules are computed once per request and stored in ViewData

## Examples

Check out the `examples` directory for complete working examples:
- Basic registration form
- Product management with all validator types
- Nested complex types
- Custom validators

## Requirements

- .NET 9.0 or higher
- FluentValidation 11.0 or higher
- jQuery 3.x
- jQuery Validation 1.19.x
- jQuery Validation Unobtrusive 3.2.x

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built for ASP.NET Core 9
- Integrates with [FluentValidation](https://github.com/FluentValidation/FluentValidation)
- Works seamlessly with jQuery Unobtrusive Validation

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/MKorain/FluentValidation.AspNetCore.TagHelpers/issues).

---

Made with ❤️ by [MKorain](https://github.com/MKorain)