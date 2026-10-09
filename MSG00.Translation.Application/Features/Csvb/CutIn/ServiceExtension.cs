using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.CutIn;

public static class ServiceExtension
{
    public static IServiceCollection AddCutIn(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}