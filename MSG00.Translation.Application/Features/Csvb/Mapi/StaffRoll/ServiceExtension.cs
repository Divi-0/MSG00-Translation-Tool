using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

public static class ServiceExtension
{
    public static IServiceCollection AddMapiStaffRoll(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}