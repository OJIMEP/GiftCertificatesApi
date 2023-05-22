using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GiftCertificates.Application.Database;
using GiftCertificates.Application.Repositories;

namespace GiftCertificates.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

            services.AddSingleton<IGiftCertificatesRepository, GiftCertificatesRepository>();

            return services;
        }
    }
}
