using BoletoNetCore.WebAPI.Models;
using Microsoft.AspNetCore.Hosting.Server;
using System.Collections;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using System.Net.NetworkInformation;

namespace BoletoNetCore.WebAPI.Extensions
{
    public class GerarBoletoBancos
    {
        readonly IBanco _banco;

        public GerarBoletoBancos(IBanco banco)
        {
            _banco = banco;
        }

        public string RetornarHtmlBoleto(DadosBoleto dadosBoleto)
        {
            // 1º Beneficiarios = Quem recebe o pagamento
            Beneficiario beneficiario = new Beneficiario()
            {
                CPFCNPJ = dadosBoleto.BeneficiarioResponse.CPFCNPJ,
                Nome = dadosBoleto.BeneficiarioResponse.Nome,
                ContaBancaria = new ContaBancaria()
                {
                    Agencia = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.Agencia,
                    Conta = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.Conta,
                    CarteiraPadrao = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.CarteiraPadrao,
                    TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                    TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                    TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa,
                    DigitoAgencia = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.DigitoAgencia,
                    DigitoConta = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.DigitoConta,
                    NossoNumeroBancoCorrespondente = dadosBoleto.NossoNumero,
                    LocalPagamento = dadosBoleto.BeneficiarioResponse.ContaBancariaResponse.LocalPagamento,

                },
                Observacoes = dadosBoleto.BeneficiarioResponse.Observacoes,
                MostrarCNPJnoBoleto = dadosBoleto.BeneficiarioResponse.MostrarCNPJnoBoleto
            };

            _banco.Beneficiario = beneficiario;
            _banco.FormataBeneficiario();

            var boleto = GerarBoleto(_banco, dadosBoleto);
            var boletoBancario = new BoletoBancario();
            boletoBancario.Boleto = boleto;

            var email = boletoBancario.HtmlBoletoParaEnvioEmail();

            EnviarEmail(boletoBancario);

            return boletoBancario.MontaHtmlEmbedded();
        }

        private void EnviarEmail(BoletoBancario boletoBancario)
        {
            MailMessage message = new MailMessage(
        "andres.morales@umusic.com",
        "sumaya@acrj.org.br,andres1@outlook.com",
        "Teste - " + boletoBancario.Boleto.Pagador.Nome + " " + boletoBancario.Boleto.Pagador.CPFCNPJ,
        "Por favor desconsiderar este Texto") ;

            ContentType mimeType = new System.Net.Mime.ContentType("text/html");
            // Add the alternate body to the message.

            AlternateView alternate = boletoBancario.HtmlBoletoParaEnvioEmail();

            //AlternateView alternate = AlternateView.CreateAlternateViewFromString(body, mimeType);
            message.AlternateViews.Add(alternate);

            // Send the message.
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtphost.global.umusic.net";
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
            message.Dispose();
            smtp.Dispose();

            try
            {
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateMessageWithMultipleViews(): {0}",
                    ex.ToString());
            }
            // Display the values in the ContentType for the attachment.
            ContentType c = alternate.ContentType;
            Console.WriteLine("Content type");
            Console.WriteLine(c.ToString());
            Console.WriteLine("Boundary {0}", c.Boundary);
            Console.WriteLine("CharSet {0}", c.CharSet);
            Console.WriteLine("MediaType {0}", c.MediaType);
            Console.WriteLine("Name {0}", c.Name);
            Console.WriteLine("Parameters: {0}", c.Parameters.Count);
            foreach (DictionaryEntry d in c.Parameters)
            {
                Console.WriteLine("{0} = {1}", d.Key, d.Value);
            }
            Console.WriteLine();
            alternate.Dispose();
        }

        public static Boleto GerarBoleto(IBanco iBanco, DadosBoleto dadosBoleto)
        {
            try
            {
                var boleto = new Boleto(iBanco)
                {
                    Pagador = GerarPagador(dadosBoleto),
                    DataEmissao = dadosBoleto.DataEmissao,
                    DataProcessamento = dadosBoleto.DataProcessamento,
                    DataVencimento = dadosBoleto.DataVencimento,
                    ValorTitulo = dadosBoleto.ValorTitulo,
                    NossoNumero = dadosBoleto.NossoNumero,
                    NumeroDocumento = dadosBoleto.NumeroDocumento,
                    EspecieDocumento = TipoEspecieDocumento.DS,
                    ImprimirValoresAuxiliares = false,
                    ImprimirMensagemInstrucao = true,
                    MensagemInstrucoesCaixa = dadosBoleto.CampoLivre
                };

                //  Para teste não é preciso validar os dados, pois com dados falso nunca vai gerar um boleto que dê para pagar
                boleto.ValidarDados();
                return boleto;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static Pagador GerarPagador(DadosBoleto dadosBoleto)
        {
            return new Pagador
            {
                Nome = dadosBoleto.PagadorResponse.Nome,
                CPFCNPJ = dadosBoleto.PagadorResponse.CPFCNPJ,
                Observacoes = dadosBoleto.PagadorResponse.Observacoes,
                Endereco = new Endereco
                {
                    LogradouroEndereco = dadosBoleto.PagadorResponse.EnderecoResponse.Logradouro,
                    LogradouroNumero = dadosBoleto.PagadorResponse.EnderecoResponse.Numero,
                    Bairro = dadosBoleto.PagadorResponse.EnderecoResponse.Bairro,
                    Cidade = dadosBoleto.PagadorResponse.EnderecoResponse.Cidade,
                    UF = dadosBoleto.PagadorResponse.EnderecoResponse.Estado,
                    CEP = dadosBoleto.PagadorResponse.EnderecoResponse.CEP,
                }
            };
        }
    }
}
