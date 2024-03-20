using BoletoNetCore.WebAPI.Extensions;
using BoletoNetCore.WebAPI.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using Document = BoletoNetCore.WebAPI.Models.Document;
using BoletoNetCore.WebAPI.Repository;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoletoNetCore.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoletosController : ControllerBase
    {
        MetodosUteis metodosUteis = new MetodosUteis();
        IConfiguration configuration { get; set; }

        public BoletosController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }


        /// <summary>
        /// Endpoint para retornar o HTML do boleto do banco ITAU.
        /// </summary>
        /// <remarks>
        /// ## Carteiras:
        ///- Banrisul (041) - Carteira 1
        ///- Bradesco (237) - Carteira 09
        ///- Brasil (001) - Carteira 17 (Variações 019 027 035)
        ///- Caixa Econômica Federal (104) - Carteira SIG14
        ///- Cecred/Ailos (085) - Carteira 1
        ///- Itau (341) - Carteira 109, 112
        ///- Safra (422) - Carteira 1
        ///- Santander (033) - Carteira 101
        ///- Sicoob (756) - Carteira 1-01
        ///- Sicredi (748) - Carteira 1-A
        ///
        /// ## Tipo de banco emissor
        /// O tipo de banco deve ser informado dentro do parâmetro para que nossa API possa identificar de que banco se trata
        /// - BancoDoBrasil = 001
        /// - BancoDoNordeste = 004
        /// - Santander = 033
        /// - Banrisul = 041
        /// - UniprimeNortePR = 084
        /// - Cecred = 085
        /// - Caixa = 104
        /// - Bradesco = 237
        /// - Itau = 341
        /// - Safra = 422
        /// - Sicredi = 748
        /// - Sicoob = 756
        /// </remarks>
        /// <returns>Retornar o HTML do boleto.</returns>
        /*[ProducesResponseType(typeof(DadosBoleto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("GerarBoletos")]*/
        private IActionResult PostGerarBoletos(DadosBoleto dadosBoleto, int tipoBancoEmissor, string emailTo, string fileName)
        {

            try
            {
                if (dadosBoleto.BeneficiarioResponse.CPFCNPJ == null || (dadosBoleto.BeneficiarioResponse.CPFCNPJ.Length != 11 && dadosBoleto.BeneficiarioResponse.CPFCNPJ.Length != 14))
                {
                    var retorno = metodosUteis.RetornarErroPersonalizado((int)HttpStatusCode.BadRequest, "Requisição Inválida", "CPF/CNPJ inválido: Utilize 11 dígitos para CPF ou 14 para CNPJ.", "/api/Boletos/BoletoItau");
                    return BadRequest(retorno);
                }

                if (string.IsNullOrWhiteSpace(dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.CarteiraPadrao))
                {
                    var retorno = metodosUteis.RetornarErroPersonalizado((int)HttpStatusCode.BadRequest, "Requisição Inválida", "Favor informar a carteira do banco.", "/api/Boletos/BoletoItau");
                    return BadRequest(retorno);
                }

                GerarBoletoBancos gerarBoletoBancos = new GerarBoletoBancos(Banco.Instancia(metodosUteis.RetornarBancoEmissor(tipoBancoEmissor)));
                var htmlBoleto = gerarBoletoBancos.RetornarHtmlBoleto(dadosBoleto, emailTo,fileName);

                return Content(htmlBoleto, "text/html");
            }
            catch (Exception ex)
            {
                var retorno = metodosUteis.RetornarErroPersonalizado((int)HttpStatusCode.InternalServerError, "Requisição Inválida", $"Detalhe do erro: {ex.Message}", string.Empty);
                return StatusCode(StatusCodes.Status500InternalServerError, retorno);
            }
        }

        private List<CustomerAddress> ReadExcelAddress(IFormFile file)
        {
            var addresses = new List<CustomerAddress>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                stream.Position = 0;
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    reader.Read();
                    while (reader.Read()) //Each row of the file
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
            }
            return addresses;
        }

        [HttpPost("GerarBoletosPorRemessa")]
        public IActionResult GerarBoletosPorRemessa(IFormFile file)//, IFormFile dadosClientes)
        {
            string fileName = file.FileName.Replace(".REM","");
            string filePath = $"Files/{DateTime.Now.ToString("yyyy-MMM",new CultureInfo("pt-BR"))}/{fileName}";
            DirectoryInfo directoryInfo = new DirectoryInfo(filePath);

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            else
            {
                foreach (FileInfo it in directoryInfo.GetFiles())
                {
                    it.Delete();
                }
            }

            var error = new StringBuilder();
            var result = new StringBuilder();

            var remessa = FileInterface.GetRemessa(file, configuration);

            for (int i = 0; i < remessa.Linhas.Count; i++)
            {
                var it = remessa.Linhas[i];
                try
                {                    
                    string textReference = it.TextReference;

                    ContaBancariaResponse contaBancariaResponse = new ContaBancariaResponse
                    {
                        Agencia = it.BankInfo.Agency,
                        CarteiraPadrao = it.BankInfo.DefaultWallet,
                        Conta = it.BankInfo.Account,
                        ContaBancariaId = 1,
                        DigitoAgencia = it.BankInfo.AgencyDigit,
                        DigitoConta = it.BankInfo.AccountDigit,
                        LocalPagamento = "PAGÁVEL EM QUALQUER BANCO ATÉ O VENCIMENTO",
                        MensagemFixaPagador = textReference,
                        MensagemFixaTopoBoleto = textReference,
                        OperacaoConta = "",
                        TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                        TipoDistribuicao = TipoDistribuicaoBoleto.ClienteDistribui,
                        TipoDocumento = TipoDocumento.Tradicional,
                        TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                        TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa
                    };

                    BeneficiarioResponse beneficiarioResponse = new BeneficiarioResponse
                    {
                        BeneficiarioResponseId = i,
                        ContaBancariaResponse = contaBancariaResponse,
                        CPFCNPJ = "33611617000100",
                        MostrarCNPJnoBoleto = true,
                        Nome = "ACRJ - ASSOCIAÇÃO COMERCIAL DO RIO DE JANEIRO",
                        Observacoes = textReference
                    };

                    EnderecoResponse enderecoResponse = new EnderecoResponse
                    {
                        Bairro = it.CustomerAddress.Neighbourhood,
                        CEP = it.CustomerAddress.PostalCode,
                        Cidade = it.CustomerAddress.City,
                        EnderecoId = i,
                        Estado = it.CustomerAddress.State,
                        Logradouro = it.CustomerAddress.Address,
                    };

                    PagadorResponse pagadorResponse = new PagadorResponse
                    {
                        CPFCNPJ = it.TaxId,
                        EnderecoResponse = enderecoResponse,
                        Nome = it.CustomerAddress.Name,
                        Observacoes = "",
                        PagadorResponseId = i
                    };

                    DadosBoleto dadosBoleto = new DadosBoleto
                    {
                        BeneficiarioResponse = beneficiarioResponse,
                        CampoLivre = textReference,
                        DataEmissao = it.CreatedDate,
                        DataProcessamento = DateTime.Now,
                        DataVencimento = it.DueDate,
                        NossoNumero = it.ReferenceNumber,
                        NumeroDocumento = it.DocumentNumber,
                        PagadorResponse = pagadorResponse,
                        ValorTitulo = it.Value
                    };

                    var html = PostGerarBoletos(dadosBoleto, 237, "a@a.com",filePath);

                    result.AppendLine(it.DocumentNumber);
                }

                catch (Exception e)
                {
                    error.AppendLine(it.DocumentNumber);
                }
            }
            return Ok(error.ToString());
        }
    }
}
