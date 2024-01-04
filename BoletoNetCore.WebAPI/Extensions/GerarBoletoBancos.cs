using BoletoNetCore.WebAPI.Models;
using Microsoft.AspNetCore.Hosting.Server;
using System.Collections;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using System.Net.NetworkInformation;
using BoletoNetCore.Pdf.BoletoImpressao;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using Syncfusion.HtmlConverter;
using Syncfusion.Pdf;
using Syncfusion.Drawing;
using System.Drawing.Printing;
using Syncfusion.Pdf.Graphics;

namespace BoletoNetCore.WebAPI.Extensions
{
    public class GerarBoletoBancos
    {
        readonly IBanco _banco;

        public GerarBoletoBancos(IBanco banco)
        {
            _banco = banco;
        }

        public string RetornarHtmlBoleto(DadosBoleto dadosBoleto,string emailTo, bool sendEmail = false)
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

            EnviarEmail(boletoBancario,emailTo,sendEmail);

            return boletoBancario.MontaHtmlEmbedded();
        }

        private void EnviarEmail(BoletoBancario boletoBancario,string emailTo, bool sendEmail = false)
        {

            MailMessage message = new MailMessage(
            "sumaya@acrj.org.br",
            emailTo,
            "ACRJ - Comunicado referente ao Boleto do mês de Janeiro 2024", "");

            message.Bcc.Add("sumaya@acrj.org.br");

            string filename = "";

            string html = boletoBancario.MontaHtmlEmbedded(false, true, null);

            int index = html.IndexOf("<td class=\"imgLogo Al\"><img src=\"");

            index += "<td class=\"imgLogo Al\"><img src=\"".Length;

            int index2 = html.IndexOf("\" /></td>", index);

            string content = html.Substring(index, index2 - index);

            string logoAcrj = "<img src=\"data:image/jpeg;base64,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\" style=\"height:97px; width:85px\" />";

            html = html.Replace("!PUTIMAGE!", logoAcrj);

            html = html.Replace("!PUTADDRESS!",boletoBancario.Boleto.Pagador.Nome + "<br/><br/>" + boletoBancario.Boleto.Pagador.Endereco.GetEnderecoCompleto());

            string image = "data:image/jpg;base64,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";

            html = html.Replace(content,
                image);

            message.Body = @$"

<p>Prezado(a) Associado(a) {boletoBancario.Boleto.Pagador.Nome.Trim()},<br />
<br />
Bom dia,<br />
<br />
Conforme comunicamos recentemente, houve um problema no boleto banc&aacute;rio de Janeiro de 2024.<br />
<br />
Estamos emitindo um novo boleto para pagamento, o mesmo se encontra em anexo.<br />
<br />
Caso o pagamento j&aacute; tenha sido efetuado, por favor desconsiderar este email.</p>

<p>&nbsp;</p>

<p>&nbsp;</p>

<p><img src=""data:image/jpeg;base64,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"" style=""height:97px; width:85px"" /><strong>Sumay&aacute; Medina</strong></p>

<p>DESOC &ndash; Departamento de Associados</p>

<p>Associa&ccedil;&atilde;o Comercial do Rio de Janeiro</p>

<p>(21) 2514-1281</p>

<p><a href=""http://www.acrj.org.br/"">www.acrj.org.br</a></p>

<p><strong><em>&ldquo;O melhor lugar do Rio para fazer neg&oacute;cios.&rdquo;</em></strong></p>

<p><strong>Imprima este documento somente se necess&aacute;rio.</strong></p>

<p><strong>Menos papel, mais &aacute;rvores.</strong></p>

";
            message.IsBodyHtml = true;

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            //filename = $"Files/{boletoBancario.Boleto.NumeroDocumento}-{DateTime.Now.ToString("yyyyMMddHHmmss")}.pdf";

            filename = $"Files/{boletoBancario.Boleto.NumeroDocumento}-{boletoBancario.Boleto.Pagador.Nome.Trim().Replace("/","").Replace("\\","")}.pdf";

            //Initialize HTML to PDF converter.
            HtmlToPdfConverter htmlConverter = new HtmlToPdfConverter();

            htmlConverter.ConverterSettings.Margin.All = 0;

            //Initialize blink converter settings. 
            BlinkConverterSettings blinkConverterSettings = new BlinkConverterSettings();
            //Set Blink viewport size.
            blinkConverterSettings.ViewPortSize = new Size(0, 0);
            //Assign Blink converter settings to HTML converter.
            htmlConverter.ConverterSettings = blinkConverterSettings;


            //Convert URL to PDF.

            PdfDocument document = htmlConverter.Convert(html, null);


            FileStream fileStream = new FileStream(filename, FileMode.CreateNew, FileAccess.ReadWrite);
            //Save and close the PDF document.
            document.Save(fileStream);
            document.Close(true);
            fileStream.Close();

            message.BodyEncoding = Encoding.UTF8;

            Attachment attachment = new Attachment(filename);

            message.Attachments.Add(attachment);

            // Send the message.
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "email-ssl.com.br";
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential("sumaya@acrj.org.br", "sumaacrJ@#1012");
            //smtp.EnableSsl = true;
            if(sendEmail)
                smtp.Send(message);
            message.Dispose();
            smtp.Dispose();
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
