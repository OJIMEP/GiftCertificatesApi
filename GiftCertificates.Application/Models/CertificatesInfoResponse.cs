using System.Text.Json.Serialization;

namespace GiftCertificates.Application.Models
{
    public class CertificatesInfoResponse
    {
        [JsonPropertyName("certificates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CertificateInfoResponse[]? Certificates { get; set; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CertPostError[]? Errors { get; set; }

        private List<CertificateInfoResponse>? _certificates;
        private List<CertPostError>? _errors;

        public void AddError(string barcode, int code, string title)
        {
            if (_errors is null)
            {
                _errors = new List<CertPostError>();
            }

            _errors.Add(new CertPostError
            {
                Barcode = barcode,
                Code = code,
                Title = title
            });

            Errors = _errors.ToArray();
        }

        public void AddCertificate(string barcode, decimal sum)
        {
            if (_certificates is null)
            {
                _certificates = new List<CertificateInfoResponse>();
            }

            _certificates.Add(new CertificateInfoResponse
            {
                Barcode = barcode,
                Sum = sum
            });

            Certificates = _certificates.ToArray();
        }

        public class CertPostError
        {
            [JsonPropertyName("barcode")]
            public string? Barcode { get; set; }
            [JsonPropertyName("code")]
            public int Code { get; set; }
            [JsonPropertyName("title")]
            public string? Title { get; set; }
        }
    }
}
