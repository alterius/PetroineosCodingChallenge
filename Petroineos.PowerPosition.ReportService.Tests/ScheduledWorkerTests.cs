using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Petroineos.PowerPosition.ReportService.Tests
{
    public class ScheduledWorkerTests
    {
        private readonly FakeTimeProvider _timeProvider;

        public ScheduledWorkerTests()
        {
            _timeProvider = new FakeTimeProvider();
        }

        [Fact]
        public async Task Run_IsExecutedEveryMinute()
        {
            var worker = Substitute.ForPartsOf<ScheduledWorker>(
                _timeProvider,
                "*/1 * * * *",
                NullLogger<ScheduledWorker>.Instance,
                false);

            var startTime = _timeProvider.GetUtcNow();
            _timeProvider.Advance(TimeSpan.FromSeconds(30));

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var tcs = new TaskCompletionSource();
            var runReceivedTriggerDateTimes = new ConcurrentBag<DateTimeOffset>();

            worker.GetType().GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(worker, [Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()])
                .Returns(Task.CompletedTask)
                .AndDoes((c) =>
                {
                    runReceivedTriggerDateTimes.Add((DateTimeOffset)c[0]);
                    if (runReceivedTriggerDateTimes.Count > 3)
                    {
                        tcs.TrySetResult();
                    }
                });

            _ = Task.Run(async () =>
            {
                await worker.StartAsync(CancellationToken.None);

                while (!tcs.Task.IsCompleted)
                {
                    _timeProvider.Advance(TimeSpan.FromSeconds(10));

                    if (cts.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                    }

                    await Task.Delay(1);
                }
            });

            await tcs.Task;

            runReceivedTriggerDateTimes.Should().Contain(
            [
                startTime.AddMinutes(1),
                startTime.AddMinutes(2),
                startTime.AddMinutes(3)
            ]);
        }
    }
}
