using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Services;

namespace Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes
{
    public class IntraDayVolumesReportWorker : ScheduledWorker
    {
        private static readonly TimeZoneInfo _londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        private readonly IntraDayVolumesReportGenerator _reportGenerator;
        private readonly IntraDayVolumesReportSettings _settings;
        private readonly ILogger<IntraDayVolumesReportWorker> _logger;
        private readonly ResiliencePipeline _resiliencePipeline;

        public IntraDayVolumesReportWorker(
            IntraDayVolumesReportGenerator reportGenerator,
            TimeProvider timeProvider,
            IOptions<IntraDayVolumesReportSettings> settings,
            ILogger<IntraDayVolumesReportWorker> logger)
            : base(timeProvider, settings.Value.Schedule, logger, true)
        {
            _reportGenerator = reportGenerator;
            _settings = settings.Value;
            _logger = logger;
            _resiliencePipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<PowerServiceException>(),
                    MaxRetryAttempts = 3,
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            args.Outcome.Exception,
                            "Attempt {attempt} to generate report failed with PowerServiceException. Retrying in {retryDelay}.",
                            args.AttemptNumber + 1,
                            args.RetryDelay);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();

            if (!string.IsNullOrWhiteSpace(_settings.ExportLocation) && !Directory.Exists(_settings.ExportLocation))
            {
                Directory.CreateDirectory(_settings.ExportLocation);
            }
        }

        protected override async Task Run(DateTimeOffset triggerDateTimeUtc, CancellationToken stoppingToken)
        {
            var localTime = TimeZoneInfo.ConvertTime(triggerDateTimeUtc, _londonTimeZone);

            var report = await _resiliencePipeline.ExecuteAsync(
                async _ => await _reportGenerator.GenerateReport(localTime.DateTime, _londonTimeZone), CancellationToken.None);

            var path = Path.Combine(
                _settings.ExportLocation,
                $"PowerPosition_{localTime:yyyyMMdd}_{localTime:HHmm}.csv");

            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(report.Volumes, CancellationToken.None);
            }

            _logger.LogInformation("Successfully generated IntraDayVolumesReport at '{reportLocation}'.", Path.GetFullPath(path));
        }
    }
}
