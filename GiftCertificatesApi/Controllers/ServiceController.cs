using GiftCertificates.Application.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GiftCertificates.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ServiceController(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        [Route("HealthCheck")]
        [HttpGet]
        public async Task<IActionResult> HealthCheckAsync(CancellationToken token)
        {
            SqlConnection conn;

            try
            {
                var dbConnection = await _dbConnectionFactory.CreateConnectionAsync(token);
                conn = dbConnection.Connection;
            }
            catch
            {
                return StatusCode(500);
            }

            if (conn is null)
            {
                Dictionary<string, string> errorDesc = new()
                {
                    { "ErrorDescription", "Не найдено доступное соединение к БД" }
                };

                return StatusCode(500, errorDesc);
            }

            await conn.CloseAsync();
            return StatusCode(200, new { Status = "Ok" });
        }
    }
}
