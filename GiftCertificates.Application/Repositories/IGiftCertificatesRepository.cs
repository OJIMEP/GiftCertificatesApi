using GiftCertificates.Application.Models;

namespace GiftCertificates.Application.Repositories
{
    public interface IGiftCertificatesRepository
    {
        Task<List<CertificateInfoResult>> GetCertificatesInfoByListAsync(List<string> barcodes, CancellationToken token);
    }
}
