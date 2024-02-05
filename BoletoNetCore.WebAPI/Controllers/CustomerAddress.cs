namespace BoletoNetCore.WebAPI.Controllers
{
    public class CustomerAddress
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }              
        public string Neighbourhood { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string TaxId { get; set; }
    }
}