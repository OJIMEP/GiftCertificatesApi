using GiftCertificates.Application.Database;
using GiftCertificates.Application.Models;
using GiftCertificates.Application.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace GiftCertificates.Application.Repositories
{
    public class GiftCertificatesRepository : IGiftCertificatesRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IHttpContextAccessor _contextAccessor;

        public GiftCertificatesRepository(IDbConnectionFactory dbConnectionFactory, IHttpContextAccessor contextAccessor)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _contextAccessor = contextAccessor;
        }

        public async Task<List<CertificateInfoResult>> GetCertificatesInfoByListAsync(List<string> barcodes, CancellationToken token)
        {
            var result = new List<CertificateInfoResult>();

            DbConnection dbConnection = await _dbConnectionFactory.GetDbConnection(token);

            using (var connection = dbConnection.Connection)
            {
                SqlCommand command = CertificatesInfoCommand(connection, barcodes);

                var watch = Stopwatch.StartNew();

                SqlDataReader dr = await command.ExecuteReaderAsync(token);

                if (dr.HasRows)
                {
                    while (await dr.ReadAsync(token))
                    {
                        var record = new CertificateInfoResult
                        {
                            Barcode = dr.GetString("Barcode"),
                            Sum = dr.GetDecimal("SumLeft"),
                            IsActive = dr.GetInt32("IsActive") == 1,
                            IsValid = dr.GetInt32("IsValid") == 1
                        };

                        result.Add(record);
                    }
                }

                _ = dr.CloseAsync();

                watch.Stop();

                lock (_contextAccessor.HttpContext.Items)
                {
                    _contextAccessor.HttpContext.Items["DatabaseConnection"] = dbConnection.ConnectionWithoutCredentials;
                    _contextAccessor.HttpContext.Items["TimeDatabaseConnection"] = dbConnection.ConnectTimeInMilliseconds;
                    _contextAccessor.HttpContext.Items["TimeSqlExecution"] = watch.ElapsedMilliseconds;
                }
            }

            result.ForEach(x => x.Barcode = barcodes.Find(b => b.ToUpper() == x.Barcode) ?? x.Barcode);

            foreach (var barcode in barcodes)
            {
                if (result.Find(x => x.Barcode == barcode) == null)
                {
                    result.Add(new CertificateInfoResult
                    {
                        Barcode = barcode,
                        NotFound = true
                    });
                }
            }

            return result;
        }

        private static SqlCommand CertificatesInfoCommand(SqlConnection connection, List<string> barcodes)
        {
            List<string> barcodesUpperCase = barcodes.Select(x => x.ToUpper()).Distinct().ToList();

            SqlCommand command = new()
            {
                Connection = connection,
                CommandTimeout = 5
            };

            List<string> barcodeParameters = new();
            for (int i = 0; i < barcodesUpperCase.Count; i++)
            {
                var parameterString = $"@Barcode{i}";
                barcodeParameters.Add(parameterString);
                command.Parameters.Add(parameterString, SqlDbType.NVarChar, 12);
                command.Parameters[parameterString].Value = barcodesUpperCase[i];
            }

            command.CommandText = GiftCertificatesQueries.CertInfo.Replace("@Barcode", string.Join(",", barcodeParameters));

            command.Parameters.Add("@DateNow", SqlDbType.DateTime);
            command.Parameters["@DateNow"].Value = DateTime.Now.AddMonths(24000);

            command.Parameters.Add("@EmptyDate", SqlDbType.DateTime);
            command.Parameters["@EmptyDate"].Value = new DateTime(2001, 1, 1, 0, 0, 0);

            return command;
        }
    }
}
