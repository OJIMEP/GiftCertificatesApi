using FluentValidation;
using GiftCertificates.Api.Filters;
using GiftCertificates.Api.Mapping;
using GiftCertificates.Api.Validators;
using GiftCertificates.Application.Models;
using GiftCertificates.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftCertificates.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
    [ServiceFilter(typeof(LogActionFilter))]
    public class GiftCertController : ControllerBase
    {
        private readonly IGiftCertificatesRepository _giftCertificatesRepository;
        private readonly BarcodeValidator _barcodeValidator;

        public GiftCertController(IGiftCertificatesRepository giftCertificatesRepository, BarcodeValidator barcodeValidator)
        {
            _giftCertificatesRepository = giftCertificatesRepository;
            _barcodeValidator = barcodeValidator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CertificatesInfoResponse))]
        [Authorize]
        public async Task<IActionResult> GetCertificatesInfoAsync(List<string> request, CancellationToken token = default)
        {
            await _barcodeValidator.ValidateAndThrowAsync(request, token);

            var result = await _giftCertificatesRepository.GetCertificatesInfoByListAsync(request, token);

            return Ok(result.MapToCertificatesInfoResponse());
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CertificateInfoResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> GetCertificatesInfoAsync(string barcode, CancellationToken token = default)
        {
            var request = new List<string> { barcode };

            await _barcodeValidator.ValidateAndThrowAsync(request, token);

            var result = (await _giftCertificatesRepository.GetCertificatesInfoByListAsync(request, token)).First();

            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.IsValid)
            {
                return StatusCode(400, "Срок действия сертификата истек");
            }

            if (!result.IsActive)
            {
                return StatusCode(400, "Сертификат не активен");
            }

            return Ok(result.MapToCertificateInfoResponse());
        }
    }
}
