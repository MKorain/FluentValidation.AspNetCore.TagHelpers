namespace FluentValidation.AspNetCore.TagHelpers.Examples.Models
{
    public class OrderModel
    {
        public string OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public AddressModel ShippingAddress { get; set; }
        public AddressModel BillingAddress { get; set; }
    }

    public class AddressModel
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }
}