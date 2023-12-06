using BoletoNetCore.WebAPI.Extensions;
using BoletoNetCore.WebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;
using System.Text;

namespace BoletoNetCore.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoletosController : ControllerBase
    {
        MetodosUteis metodosUteis = new MetodosUteis();

        public BoletosController()
        {
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
        [ProducesResponseType(typeof(DadosBoleto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("GerarBoletos")]
        public IActionResult PostGerarBoletos(DadosBoleto dadosBoleto, int tipoBancoEmissor)
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
                var htmlBoleto = gerarBoletoBancos.RetornarHtmlBoleto(dadosBoleto);

                return Content(htmlBoleto, "text/html");
            }
            catch (Exception ex)
            {
                var retorno = metodosUteis.RetornarErroPersonalizado((int)HttpStatusCode.InternalServerError, "Requisição Inválida", $"Detalhe do erro: {ex.Message}", string.Empty);
                return StatusCode(StatusCodes.Status500InternalServerError, retorno);
            }
        }

        [HttpPost("GerarBoletosPorRemessa")]
        public IActionResult GerarBoletosPorRemessa(IFormFile fileRaw)
        {
            var stream = fileRaw.OpenReadStream();

            byte[] bytes = new byte[stream.Length];

            stream.Read(bytes);

            var lines = Encoding.Default.GetString(bytes).Split(Environment.NewLine);

            var result = new StringBuilder();            

            for(int i = 1; i < lines.Length - 1; i++) //Pula a primeira e a última
            {
                string line = lines[i];

                ContaBancariaResponse contaBancariaResponse = new ContaBancariaResponse {
                    Agencia = line.Substring(25, 4),
                    CarteiraPadrao = line.Substring(22,2),
                    Conta = line.Substring(30, 6),
                    ContaBancariaId = 1,
                    DigitoAgencia = line.Substring(29,1),
                    DigitoConta = line.Substring(36, 1),
                    LocalPagamento = "PAGÁVEL EM QUALQUER BANCO ATÉ O VENCIMENTO",
                    MensagemFixaPagador = "CONTRIBUIÇÃO ASSOCIATIVA REFERENTE DEZEMBRO / 2023.\r\nSUA CONTRIBUIÇÃO É MUITO IMPORTANTE PARA MANUTENÇÃO E QUALIDADE DE \r\nNOSSOS SERVIÇOS. \r\nA ACRJ OFERECE SALAS COMERCIAIS PARA LOCAÇÃO DE 24 A 720 M² . \r\nTel: (21) 2514-1212 ou locacao@acrj.org.br",
                    MensagemFixaTopoBoleto = "CONTRIBUIÇÃO ASSOCIATIVA REFERENTE DEZEMBRO / 2023.\r\nSUA CONTRIBUIÇÃO É MUITO IMPORTANTE PARA MANUTENÇÃO E QUALIDADE DE \r\nNOSSOS SERVIÇOS. \r\nA ACRJ OFERECE SALAS COMERCIAIS PARA LOCAÇÃO DE 24 A 720 M² . \r\nTel: (21) 2514-1212 ou locacao@acrj.org.br",
                    OperacaoConta = "",
                    TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                    TipoDistribuicao = TipoDistribuicaoBoleto.ClienteDistribui,
                    TipoDocumento = TipoDocumento.Tradicional,
                    TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                    TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa                    
                };

                Endereco endereco = new Endereco { 
                    Bairro = "",
                    CEP = "",
                    Cidade = "",
                    LogradouroComplemento = "",
                    LogradouroEndereco = "",
                    LogradouroNumero = "",
                    UF = ""
                };

                BeneficiarioResponse beneficiarioResponse = new BeneficiarioResponse {
                    BeneficiarioResponseId = i,
                    ContaBancariaResponse = contaBancariaResponse,
                    CPFCNPJ = "33611617000100",
                    //Endereco = endereco,
                    MostrarCNPJnoBoleto = true,
                    Nome = "ACRJ - ASSOCIAÇÃO COMERCIAL DO RIO DE JANEIRO",
                    Observacoes = "CONTRIBUIÇÃO ASSOCIATIVA REFERENTE DEZEMBRO / 2023.\r\nSUA CONTRIBUIÇÃO É MUITO IMPORTANTE PARA MANUTENÇÃO E QUALIDADE DE \r\nNOSSOS SERVIÇOS. \r\nA ACRJ OFERECE SALAS COMERCIAIS PARA LOCAÇÃO DE 24 A 720 M² . \r\nTel: (21) 2514-1212 ou locacao@acrj.org.br"
                };

                EnderecoResponse enderecoResponse = new EnderecoResponse {
                    Bairro = "",
                    CEP = line.Substring(226,8),
                    Cidade = "",
                    Complemento = "",
                    EnderecoId = i,
                    Estado = "",
                    Logradouro = line.Substring(274,40),
                    Numero = ""
                };

                PagadorResponse pagadorResponse = new PagadorResponse {
                    CPFCNPJ = line.Substring(220 + (line.Substring(219,1) == "1" ? 3 : 0),11),
                    EnderecoResponse = enderecoResponse,
                    Nome = line.Substring(234,40),
                    Observacoes = "",
                    PagadorResponseId = i
                };

                DadosBoleto dadosBoleto = new DadosBoleto {
                    BeneficiarioResponse = beneficiarioResponse,
                    CampoLivre = "CONTRIBUIÇÃO ASSOCIATIVA REFERENTE DEZEMBRO / 2023.\r\nSUA CONTRIBUIÇÃO É MUITO IMPORTANTE PARA MANUTENÇÃO E QUALIDADE DE \r\nNOSSOS SERVIÇOS. \r\nA ACRJ OFERECE SALAS COMERCIAIS PARA LOCAÇÃO DE 24 A 720 M² . \r\nTel: (21) 2514-1212 ou locacao@acrj.org.br",
                    DataEmissao = DateTime.ParseExact(line.Substring(150, 6), "ddMMyy", CultureInfo.InvariantCulture),
                    DataProcessamento = DateTime.Now,
                    DataVencimento = DateTime.ParseExact(line.Substring(120,6),"ddMMyy",CultureInfo.InvariantCulture),
                    NossoNumero = line.Substring(70,11),
                    NumeroDocumento = line.Substring(110,10),
                    PagadorResponse = pagadorResponse,
                    ValorTitulo = decimal.Parse(line.Substring(126,13)) / 100
                };

                var html = PostGerarBoletos(dadosBoleto, 237);
            }            

            return Ok();
        }
    }
}
