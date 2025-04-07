using Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes;
using Services;

namespace Petroineos.PowerPosition.ReportService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<IntraDayVolumesReportWorker>();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.Configure<IntraDayVolumesReportSettings>(
                builder.Configuration.GetSection(IntraDayVolumesReportSettings.SettingsKey));

            builder.Services.AddSingleton<IPowerService, PowerService>();
            builder.Services.AddSingleton<IntraDayVolumesReportGenerator>();

            var host = builder.Build();
            host.Run();
        }
    }
}
