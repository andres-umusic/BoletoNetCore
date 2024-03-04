namespace BoletoNetCore.WebAPI.Models
{
    public class Remessa
    {
        public List<Linha> Linhas { get; set; }
    }

    public class BankInfo
    {
        public string Agency { get; set; }
        public string AgencyDigit { get; set; } 
        public string DefaultWallet { get; set; }
        public string Account { get; set; }
        public string AccountDigit { get; set; }

    }

    public class Linha 
    {
        public string DocumentNumber { get; set; }
        public string TaxId { get; set; }
        public BankInfo BankInfo { get; set; }    
        public CustomerAddress CustomerAddress { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime DueDate { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal Value { get; set; }
        public string TextReference { get; set; }
    }    
}