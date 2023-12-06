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
        "sumaya@acrj.org.br,margareth@acrj.org.br,andres1@outlook.com",
        "ACRJ - Comunicado referente ao Boleto do mês de Dezembro 2023", "");

            message.Body = @$"<p>Prezado(a) Associado(a) {boletoBancario.Boleto.Pagador.Nome.Trim()},<br />
<br />
Bom dia,<br />
<br />
Conforme comunicamos recentemente, houve um problema no boleto banc&aacute;rio de dezembro de 2023.<br />
<br />
Estamos emitindo um novo boleto para pagamento em anexo.<br />
<br />
Caso j&aacute; tenha sido efetuado favor considerar esse email.</p>

<p>&nbsp;</p>

<p><img src=""https://ckeditor.com/apps/ckfinder/userfiles/files/image-20231206095829-1.png"" style=""height:97px; width:85px"" /><strong>Sumay&aacute; Medina</strong></p>

<p>DESOC &ndash; Departamento de Associados</p>

<p>Associa&ccedil;&atilde;o Comercial do Rio de Janeiro</p>

<p>(21) 2514-1281</p>

<p><a href=""http://www.acrj.org.br/"">www.acrj.org.br</a></p>

<p><strong><em>&ldquo;O melhor lugar do Rio para fazer neg&oacute;cios.&rdquo;</em></strong></p>

<p><strong>Imprima este documento somente se necess&aacute;rio.</strong></p>

<p><strong>Menos papel, mais &aacute;rvores.</strong></p>
";

            message.IsBodyHtml = true;

            string filename = "";

            string html = boletoBancario.MontaHtmlEmbedded();

            int index = html.IndexOf("<td class=\"imgLogo Al\"><img src=\"");

            index += "<td class=\"imgLogo Al\"><img src=\"".Length;

            int index2 = html.IndexOf("\" /></td>",index);

            string content = html.Substring(index, index2 - index);

            string image = "data:image/jpg;base64,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";

            html = html.Replace(content,
                image);

            var renderer = new ChromePdfRenderer();

            var pdf = renderer.RenderHtmlAsPdf(html);

            filename = $"Files/{boletoBancario.Boleto.NumeroDocumento}.pdf";

            pdf.SaveAs(filename);

            Attachment attachment = new Attachment(filename);

            message.Attachments.Add(attachment);

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
