using BoletoNetCore.WebAPI.Models;
using System.Globalization;
using System.Security.Cryptography.Xml;
using System.Text;

namespace BoletoNetCore.WebAPI.Repository
{
    public static class FileInterface
    {
        public static Remessa GetRemessa(IFormFile file, IConfiguration configuration)
        {            

            BoletoRepository boletoRepository = new BoletoRepository(configuration);

            var addresses = boletoRepository.ReadDataBaseAddresses();//ReadExcelAddress(dadosClientes);                        

            Remessa remessa = new Remessa { Linhas = new List<Linha>()};

            var stream = file.OpenReadStream();

            var bytes = new byte[stream.Length];

            string filter = "";

            stream.Read(bytes);

            var lines = Encoding.Default.GetString(bytes).Split(Environment.NewLine);            

            for (int i = 1; i < lines.Length - 1; i++) //Pula a primeira e a última
            {
                string line = lines[i];
                string numeroDocumento = line.Substring(110, 10);
                try
                {
                    var cpf = (line.Substring(219, 1) == "1");

                    string taxId = line.Substring(220 + ((!cpf) ? 0 : 3), 11 + (cpf ? 0 : 3));

                    int j = 0;                                        

                    BankInfo bankInfo = new BankInfo 
                    {
                        Agency = line.Substring(25, 4),
                        DefaultWallet = line.Substring(22, 2),
                        Account = line.Substring(30, 6),                        
                        AgencyDigit = line.Substring(29, 1),
                        AccountDigit = line.Substring(36, 1)
                    };

                    var customerAddress = addresses.Find(x => x.TaxId == taxId);

                    if(customerAddress == null)
                    {
                        continue;
                    }

                    Linha linha = new Linha 
                    {
                        BankInfo = bankInfo,
                        CreatedDate = DateTime.ParseExact(line.Substring(150, 6), "ddMMyy", CultureInfo.InvariantCulture),
                        CustomerAddress = customerAddress,
                        DocumentNumber = numeroDocumento,
                        DueDate = DateTime.ParseExact(line.Substring(120, 6), "ddMMyy", CultureInfo.InvariantCulture),
                        ReferenceNumber = line.Substring(70, 11),
                        TaxId = taxId,
                        //TextReference = textReference,
                        Value = decimal.Parse(line.Substring(126, 13)) / 100
                    };

                    remessa.Linhas.Add(linha);

                    filter += $",'{numeroDocumento}'";
                }
                catch (Exception e)
                {
                    
                }
            }

            var documents = boletoRepository.ReadDataBaseDocuments(filter.Substring(1));

            foreach(var it in remessa.Linhas)
            {
                var document = documents.Find(x => x.DocumentNumber == it.DocumentNumber);

                string reference = document.Reference;

                string textReference = $"CONTRIBUIÇÃO ASSOCIATIVA {reference}.\r\nSUA CONTRIBUIÇÃO É MUITO IMPORTANTE PARA MANUTENÇÃO E QUALIDADE DE \r\nNOSSOS SERVIÇOS. \r\nA ACRJ OFERECE SALAS COMERCIAIS PARA LOCAÇÃO DE 24 A 720 M² . \r\nTel: (21) 2514-1212 ou locacao@acrj.org.br";

                it.TextReference = textReference;
            }

            return remessa;
        }
    }
}
