namespace FluentValidation.AspNetCore.TagHelpers.Examples.Models
{
    public class ProductModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Sku { get; set; }
        public string ManufacturerEmail { get; set; }
        public string ProductUrl { get; set; }
        public int Rating { get; set; }
        public string CreditCard { get; set; }
    }
}