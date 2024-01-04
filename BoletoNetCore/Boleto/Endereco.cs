namespace BoletoNetCore
{
    /// <summary>
    /// Representa o endereço do Beneficiário ou Pagador.
    /// </summary>
    public class Endereco
    {
        public string LogradouroEndereco { get; set; } = string.Empty;
        public string LogradouroNumero { get; set; } = string.Empty;
        public string LogradouroComplemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string UF { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;

        public string GetEnderecoCompleto()
        {
            return (!string.IsNullOrEmpty(LogradouroEndereco) ? LogradouroEndereco + ", " : "") + (!string.IsNullOrEmpty(LogradouroNumero) ? LogradouroNumero + ", " : "") + (!string.IsNullOrEmpty(LogradouroComplemento) ? LogradouroComplemento + ", " : "") + (!string.IsNullOrEmpty(Bairro) ? Bairro + ", " : "") + (!string.IsNullOrEmpty(CEP) ? " CEP: " + CEP + ", " : "") + (!string.IsNullOrEmpty(Cidade) ? Cidade + ", " : "") + (!string.IsNullOrEmpty(UF) ? UF : "");
        }

        public string FormataLogradouro(int tamanhoFinal)
        {
            var logradouroCompleto = string.Empty;
            if (!string.IsNullOrEmpty(LogradouroNumero))
                logradouroCompleto += " " + LogradouroNumero;
            if (!string.IsNullOrEmpty(LogradouroComplemento))
                logradouroCompleto += " " + (LogradouroComplemento.Length > 20 ? LogradouroComplemento.Substring(0, 20) : LogradouroComplemento);

            if (tamanhoFinal == 0)
                return LogradouroEndereco + logradouroCompleto;

            if (LogradouroEndereco.Length + logradouroCompleto.Length <= tamanhoFinal)
                return LogradouroEndereco + logradouroCompleto;

            return (LogradouroEndereco + logradouroCompleto).Substring(0, tamanhoFinal);
        }
    }
}
