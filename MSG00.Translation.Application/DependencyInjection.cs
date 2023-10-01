using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediator();

            return services;
        }
    }
}
