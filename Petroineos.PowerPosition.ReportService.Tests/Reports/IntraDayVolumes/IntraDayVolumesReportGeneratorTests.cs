using FluentAssertions;
using NSubstitute;
using Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes;
using Services;

namespace Petroineos.PowerPosition.ReportService.Tests.Reports.IntraDayVolumes
{
    public class IntraDayVolumesReportGeneratorTests
    {
        private static readonly TimeZoneInfo _londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        private readonly IPowerService _powerService;
        private readonly IntraDayVolumesReportGenerator _generator;

        public IntraDayVolumesReportGeneratorTests()
        {
            _powerService = Substitute.For<IPowerService>();
            _generator = new IntraDayVolumesReportGenerator(_powerService);
        }

        [Fact]
        public async Task GenerateReport_CorrectlyAggregatesTrades()
        {
            var date = new DateTime(2025, 7, 4, 18, 23, 5, DateTimeKind.Local);

            var trades = new List<PowerTrade>
            {
                CreateAndPopulatePowerTrade(date, 24),
                CreateAndPopulatePowerTrade(date, 24),
                CreateAndPopulatePowerTrade(date, 24)
            };

            _powerService.GetTradesAsync(Arg.Is(date.Date))
                .Returns(trades);

            var result = await _generator.GenerateReport(date, _londonTimeZone);

            result.Should().NotBeNull();
            result.ReportDate.Should().Be(DateOnly.FromDateTime(date));
            result.Volumes.Should().NotBeNullOrEmpty()
                .And.HaveCount(24);
            result.Volumes.Sum(v => v.Volume).Should().Be(trades
                .SelectMany(t => t.Periods)
                .Sum(p => (decimal)p.Volume));
            result.Volumes.Should().SatisfyRespectively(
                Enumerable.Range(1, 24).Select(i => (Action<IntraDayVolume>)((v) =>
                {
                    v.LocalTime.Should().Be(date.Date.AddHours(i - 2));
                    v.Volume.Should().Be(trades
                        .SelectMany(t => t.Periods)
                        .Where(p => p.Period == i)
                        .Sum(p => (decimal)p.Volume));
                })));
        }

        [Fact]
        public async Task GenerateReport_WithOctoberDstTransition_CorrectlyAggregatesTrades()
        {
            var date = new DateTime(2024, 10, 27, 18, 23, 5, DateTimeKind.Local);

            var trades = new List<PowerTrade>
            {
                CreateAndPopulatePowerTrade(date, 25),
                CreateAndPopulatePowerTrade(date, 25),
                CreateAndPopulatePowerTrade(date, 25)
            };

            _powerService.GetTradesAsync(Arg.Is(date.Date))
                .Returns(trades);

            var result = await _generator.GenerateReport(date, _londonTimeZone);

            result.Should().NotBeNull();
            result.ReportDate.Should().Be(DateOnly.FromDateTime(date));
            result.Volumes.Should().NotBeNullOrEmpty()
                .And.HaveCount(24);
            result.Volumes.Sum(v => v.Volume).Should().Be(trades
                .SelectMany(t => t.Periods)
                .Sum(p => (decimal)p.Volume));
            result.Volumes.Select(v => v.LocalTime).Should().OnlyHaveUniqueItems()
                .And.Equal(Enumerable.Range(1, 24).Select(i => date.Date.AddHours(i - 2)));
            result.Volumes.Single(v => v.LocalTime == date.Date.AddHours(1)).Volume.Should().Be(trades
                .SelectMany(t => t.Periods)
                .Where(p => p.Period == 3 || p.Period == 4)
                .Sum(p => (decimal)p.Volume));
        }

        [Fact]
        public async Task GenerateReport_WithMarchDstTransition_CorrectlyAggregatesTrades()
        {
            var date = new DateTime(2025, 3, 30, 18, 23, 5, DateTimeKind.Local);

            var trades = new List<PowerTrade>
            {
                CreateAndPopulatePowerTrade(date, 23),
                CreateAndPopulatePowerTrade(date, 23),
                CreateAndPopulatePowerTrade(date, 23)
            };

            _powerService.GetTradesAsync(Arg.Is(date.Date))
                .Returns(trades);

            var result = await _generator.GenerateReport(date, _londonTimeZone);

            result.Should().NotBeNull();
            result.ReportDate.Should().Be(DateOnly.FromDateTime(date));
            result.Volumes.Should().NotBeNullOrEmpty()
                .And.HaveCount(23);
            result.Volumes.Sum(v => v.Volume).Should().Be(trades
                .SelectMany(t => t.Periods)
                .Sum(p => (decimal)p.Volume));
            result.Volumes.Select(v => v.LocalTime).Should().OnlyHaveUniqueItems()
                .And.Equal(Enumerable.Range(1, 24).Except([3]).Select(i => date.Date.AddHours(i - 2)))
                .And.NotContain(date.Date.AddHours(1));
        }

        private static PowerTrade CreateAndPopulatePowerTrade(DateTime date, int numberOfPeriods)
        {
            var powerTrade = PowerTrade.Create(date, numberOfPeriods);

            foreach (var period in powerTrade.Periods)
            {
                period.Volume = Random.Shared.NextDouble();
            }

            return powerTrade;
        }
    }
}
