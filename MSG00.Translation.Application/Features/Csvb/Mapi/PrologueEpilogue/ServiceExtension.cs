using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;

public static class ServiceExtension
{
    public static IServiceCollection AddMapiPrologueEpilogue(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}