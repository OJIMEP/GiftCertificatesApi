using GiftCertificates.Application.Logging;
using GiftCertificates.Application.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GiftCertificates.Application.Database
{
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateConnectionAsync(CancellationToken token = default);

        Task<DbConnection> GetDbConnection(CancellationToken token = default);
    }

    public class SqlConnectionFactory: IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqlConnectionFactory> _logger;

        private readonly bool _checkConnection;

        public SqlConnectionFactory(IConfiguration configuration, ILogger<SqlConnectionFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _checkConnection = !_configuration.GetValue<bool>("DisableConnectionCheck");
        }

        public async Task<DbConnection> CreateConnectionAsync(CancellationToken token)
        {
            DbConnection result = new();

            var watch = Stopwatch.StartNew();

            var connectionParameters = _configuration.GetSection("OneSDatabases")
                .Get<List<DatabaseConnectionParameter>>()
                .Select(x => new DatabaseInfo(x))
                .ToList();

            var timeMs = DateTime.Now.Millisecond % 100;

            List<string> failedConnections = new();

            bool firstAvailable = false;

            var resultString = "";

            SqlConnection? connection = null;

            while (true)
            {
                int percentCounter = 0;
                foreach (var connParameter in connectionParameters)
                {
                    if (firstAvailable && failedConnections.Contains(connParameter.Connection))
                        continue;

                    percentCounter += connParameter.Priority;
                    if (timeMs <= percentCounter && connParameter.Priority != 0 || firstAvailable)
                    {
                        try
                        {
                            connection = await GetConnectionByDatabaseInfo(connParameter, token);

                            resultString = connParameter.Connection;

                            result.Connection = connection;
                            result.DatabaseType = connParameter.DatabaseType;
                            result.ConnectionWithoutCredentials = connParameter.ConnectionWithoutCredentials;
                            break;
                        }
                        catch (Exception ex)
                        {
                            var logElement = new ElasticLogElement
                            {
                                Status = LogStatus.Error,
                                ErrorDescription = ex.Message,
                                DatabaseConnection = connParameter.ConnectionWithoutCredentials
                            };

                            _logger.LogElastic(logElement);

                            if (connection != null && connection.State != System.Data.ConnectionState.Closed)
                            {
                                _ = connection.CloseAsync();
                            }

                            failedConnections.Add(connParameter.Connection);
                        }
                    }
                }

                if (resultString.Length > 0 || firstAvailable)
                    break;
                else
                    firstAvailable = true;
            }
            watch.Stop();
            result.ConnectTimeInMilliseconds = watch.ElapsedMilliseconds;

            return result;
        }

        public async Task<DbConnection> GetDbConnection(CancellationToken token = default)
        {
            DbConnection dbConnection;

            try
            {
                dbConnection = await CreateConnectionAsync(token);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            if (dbConnection.Connection == null)
            {
                throw new Exception("Не найдено доступное соединение к БД");
            }

            return dbConnection;
        }

        private async Task<SqlConnection?> GetConnectionByDatabaseInfo(DatabaseInfo databaseInfo, CancellationToken token)
        {
            SqlConnection connection = new(databaseInfo.Connection);
            await connection.OpenAsync(token);

            if (_checkConnection)
            {
                var queryStringCheck = databaseInfo.DatabaseType switch
                {
                    DatabaseType.Main => DbCheckQueries.DatabaseBalancingMain,
                    DatabaseType.ReplicaFull => DbCheckQueries.DatabaseBalancingReplicaFull,
                    DatabaseType.ReplicaTables => DbCheckQueries.DatabaseBalancingReplicaTables,
                    _ => ""
                };

                SqlCommand cmd = new(queryStringCheck, connection)
                {
                    CommandTimeout = 1
                };

                SqlDataReader dr = await cmd.ExecuteReaderAsync(token);

                _ = dr.CloseAsync();
            }

            return connection;
        }
    }
}
