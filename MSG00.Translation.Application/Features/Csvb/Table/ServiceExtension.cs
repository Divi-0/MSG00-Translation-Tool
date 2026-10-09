using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.Table;

public static class ServiceExtension
{
    public static IServiceCollection AddTable(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}