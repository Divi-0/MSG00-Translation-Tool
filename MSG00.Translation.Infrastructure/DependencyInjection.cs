using Microsoft.Extensions.DependencyInjection;
using MSG00.Translation.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Reader.Epilogue;
using MSG00.Translation.Infrastructure.Reader.EtcFgHcmHg;
using MSG00.Translation.Infrastructure.Reader.Evm;
using MSG00.Translation.Infrastructure.Reader.Prologue;
using MSG00.Translation.Infrastructure.Reader.Requirement;
using MSG00.Translation.Infrastructure.Reader.StaffRoll;
using MSG00.Translation.Infrastructure.Services;
using MSG00.Translation.Infrastructure.Writer.Epilogue;
using MSG00.Translation.Infrastructure.Writer.Etc;
using MSG00.Translation.Infrastructure.Writer.ProEpilogue;
using MSG00.Translation.Infrastructure.Writer.Requirement;
using MSG00.Translation.Infrastructure.Writer.StaffRoll;
using System.Text;

namespace MSG00.Translation.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IPrologueReader, PrologueReader>();
            services.AddSingleton<IEpilogueReader, EpilogueReader>();
            services.AddSingleton<IEtcFgHcmHgReader, EtcFgHcmHgReader>();
            services.AddSingleton<IRequirementReader, RequirementReader>();
            services.AddSingleton<IStaffRollReader, StaffRollReader>();
            services.AddSingleton<IEvmReader, EvmReader>();

            services.AddSingleton<IPrologueWriter, PrologueWriter>();
            services.AddSingleton<IEpilogueWriter, EpilogueWriter>();
            services.AddSingleton<IEtcFgHcmHgWriter, EtcFgHcmHgWriter>();
            services.AddSingleton<IRequirementWriter, RequirementWriter>();
            services.AddSingleton<IStaffRollWriter, StaffRollWriter>();

            services.AddSingleton<IConversationService, ConversationService>();
            services.AddSingleton<IPrologueService, PrologueService>();
            services.AddSingleton<IEpilogueService, EpilogueService>();
            services.AddSingleton<IEtcFgHcmHgService, EtcFgHcmHgService>();
            services.AddSingleton<IRequirementService, RequirementService>();
            services.AddSingleton<IStaffRollService, StaffRollService>();
            services.AddSingleton<IEvmService, EvmService>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return services;
        }
    }
}
