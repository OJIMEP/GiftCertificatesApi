using System.Text.Json.Serialization;

namespace GiftCertificates.Application.Models
{
    public class CertificateInfoResponse
    {
        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }
        [JsonPropertyName("sum")]
        public decimal Sum { get; set; }
    }
}
