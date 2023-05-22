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
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CertificateInfoResponse))]
        [Authorize]
        public async Task<IActionResult> GetCertificatesInfoAsync(List<string> request, CancellationToken token = default)
        {
            await _barcodeValidator.ValidateAndThrowAsync(request, token);

            var result = await _giftCertificatesRepository.GetCertificatesInfoByListAsync(request, token);

            return Ok(result.MapToAvailableDateResponse());
        }
    }
}
