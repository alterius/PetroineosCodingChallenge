using NCrontab;

namespace Petroineos.PowerPosition.ReportService
{
    public abstract class ScheduledWorker : BackgroundService
    {
        private readonly TimeProvider _timeProvider;
        private readonly CrontabSchedule _schedule;
        private readonly ILogger<ScheduledWorker> _logger;
        private readonly bool _runOnStartUp;

        protected ScheduledWorker(TimeProvider timeProvider, string schedule, ILogger<ScheduledWorker> logger, bool runOnStartUp = false)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _schedule = CrontabSchedule.Parse(
                schedule,
                new CrontabSchedule.ParseOptions
                {
                    IncludingSeconds = false
                });
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runOnStartUp = runOnStartUp;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledWorker '{workerType}' started on schedule '{schedule}'.", GetType().Name, _schedule);

            var nowUtc = _timeProvider.GetUtcNow();

            if (_runOnStartUp)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Initial run triggered for {initialDateTime}.", nowUtc);

                        await Run(nowUtc, stoppingToken);

                        _logger.LogInformation("Initial run completed for {initialDateTime}.", nowUtc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Initial run for {initialDateTime} failed with exception.", nowUtc);
                    }
                }, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                nowUtc = _timeProvider.GetUtcNow();
                var nextOccuranceUtc = _schedule.GetNextOccurrence(nowUtc.UtcDateTime);

                _logger.LogInformation("Next run scheduled for {scheduleDateTime}.", nextOccuranceUtc);

                var delay = nextOccuranceUtc - nowUtc.DateTime;
                await Task.Delay(delay, _timeProvider, stoppingToken);

                // Run on the Thread Pool so as not to block the timer schedule
                // in the event of a long-running execution.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Scheduled run triggered for {scheduleDateTime}.", nextOccuranceUtc);

                        await Run(nextOccuranceUtc, stoppingToken);

                        _logger.LogInformation("Scheduled run completed for {scheduleDateTime}.", nextOccuranceUtc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Scheduled run for {scheduleDateTime} failed with exception.", nextOccuranceUtc);
                    }
                }, stoppingToken);
            }
        }

        protected abstract Task Run(DateTimeOffset triggerDateTimeUtc, CancellationToken stoppingToken);
    }
}
