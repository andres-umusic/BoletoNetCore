using BoletoNetCore.WebAPI.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace BoletoNetCore.WebAPI.Repository
{
    public class BoletoRepository
    {
        private IConfiguration configuration;
        public BoletoRepository(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public List<CustomerAddress> ReadDataBaseAddresses()
        {
            List<CustomerAddress> addresses = new List<CustomerAddress>();

            SqlConnection cn = new SqlConnection(configuration["ConnectionString"]);

            string query = @"SELECT CGC_CPF,RAZAO_SOCIAL,ENDERECO,BAIRRO,CIDADE,ESTADO,CEP,CPF_CNPJ 
                            FROM [dbo].[Cliente] 
                            WHERE CGC_CPF LIKE 'F-%' OR CGC_CPF LIKE 'J-%'";

            SqlCommand cmd = new SqlCommand(query, cn);

            cn.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        string aux = reader.GetString(7);

                        string taxId = "";

                        if (!string.IsNullOrWhiteSpace(aux))
                        {
                            foreach (char c in aux)
                            {
                                if (Char.IsDigit(c))
                                {
                                    taxId += c;
                                }
                            }
                        }

                        CustomerAddress address = new CustomerAddress
                        {
                            Address = reader.GetString(2),
                            City = reader.GetString(4),
                            Id = reader.GetString(0),
                            Name = reader.GetString(1),
                            Neighbourhood = reader.GetString(3),
                            PostalCode = reader.GetString(6),
                            State = reader.GetString(5),
                            TaxId = taxId
                        };
                        addresses.Add(address);
                    }
                    catch { }
                }
            }

            cn.Close();

            return addresses;
        }
        public List<Document> ReadDataBaseDocuments(string filter = "")
        {
            List<Document> documents = new List<Document>();            

            SqlConnection cn = new SqlConnection(configuration["ConnectionString"]);

            string query = @$"SELECT [N_Doc]
                                 ,[Referencia]      
                             FROM [dbo].[CReceber]
                             {(string.IsNullOrWhiteSpace(filter) ? "" : "WHERE N_Doc in (" + filter + ")")}";

            SqlCommand cmd = new SqlCommand(query, cn);

            cn.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        Document document = new Document
                        {
                            DocumentNumber = reader.GetString(0),
                            Reference = reader.GetString(1)
                        };
                        documents.Add(document);
                    }
                    catch 
                    {

                    }
                }
            }

            cn.Close();

            return documents;
        }
    }
}
