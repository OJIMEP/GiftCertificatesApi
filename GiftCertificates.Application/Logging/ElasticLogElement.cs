using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace GiftCertificates.Application.Logging
{
    public class ElasticLogElement
    {
        public string? Path { get; set; }
        public string? Host { get; set; }
        public string? ResponseContent { get; set; }
        public string? RequestContent { get; set; }
        public long TimeSqlExecution { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogStatus Status { get; set; }
        public string? ErrorDescription { get; set; }
        public long TimeFullExecution { get; set; }
        public string? DatabaseConnection { get; set; }
        public string? AuthenticatedUser { get; set; }
        public long TimeDatabaseConnection { get; set; }
        public Dictionary<string, string?> AdditionalData { get; set; }

        public ElasticLogElement()
        {
            AdditionalData = new();
        }

        public void FillFromHttpContext(HttpContext httpContext)
        {
            Status = LogStatus.Ok;
            Host = httpContext.Request.Host.ToString();
            Path = httpContext.Request.Path.ToString();
            AuthenticatedUser = httpContext.User.Identity?.Name;
            AdditionalData.Add("Referer", httpContext.Request.Headers["Referer"].ToString());
            AdditionalData.Add("User-Agent", httpContext.Request.Headers["User-Agent"].ToString());
            AdditionalData.Add("RemoteIpAddress", httpContext.Connection.RemoteIpAddress.ToString());

            FillFromHttpContextItems(httpContext.Items);
        }

        public void FillFromHttpContextItems(IDictionary<object, object?> items)
        {
            try
            {
                RequestContent = items.TryGetValue("LogRequestBody", out object? request) ? request.ToString() : "";
                DatabaseConnection = items.TryGetValue("DatabaseConnection", out object? connection) ? (string)(connection ?? "") : "";
                TimeSqlExecution = items.TryGetValue("TimeSqlExecution", out object? timeSql) ? (long)(timeSql ?? 0) : 0;
                TimeDatabaseConnection = items.TryGetValue("TimeDatabaseConnection", out object? timeLoad) ? (long)(timeLoad ?? 0) : 0;
            }
            catch { }
        }
    }
}
