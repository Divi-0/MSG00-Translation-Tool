using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.Read;

public static class ServiceExtension
{
    public static IServiceCollection AddCsvbRead(this IServiceCollection services)
    {
        services.AddSingleton<ICsvbReader, CsvbReader>();

        return services;
    }
}