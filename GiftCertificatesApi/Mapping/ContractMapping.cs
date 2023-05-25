using GiftCertificates.Application.Models;

namespace GiftCertificates.Api.Mapping
{
    public static class ContractMapping
    {
        public static CertificatesInfoResponse MapToCertificatesInfoResponse(this List<CertificateInfoResult> result)
        {
            var response = new CertificatesInfoResponse();

            foreach (var certInfo in result)
            {
                if (certInfo.NotFound)
                {
                    response.AddError(certInfo.Barcode, 404, "Сертификат не существует");
                    continue;
                }

                if (!certInfo.IsValid)
                {
                    response.AddError(certInfo.Barcode, 400, "Срок действия сертификата истек");
                    continue;
                }

                //сертификат не в статусе "Активирован", "Частично погашен" или нет суммы (бывают косяки с суммами)
                if (!certInfo.IsActive || certInfo.Sum <= 0) 
                {
                    response.AddError(certInfo.Barcode, 400, "Сертификат не активен");
                    continue;
                }

                response.AddCertificate(certInfo.Barcode, certInfo.Sum);
            }

            return response;
        }

        public static CertificateInfoResponse MapToCertificateInfoResponse(this CertificateInfoResult result)
        {          
            return new CertificateInfoResponse
            {
                Barcode = result.Barcode,
                Sum = result.Sum
            };
        }
    }
}
