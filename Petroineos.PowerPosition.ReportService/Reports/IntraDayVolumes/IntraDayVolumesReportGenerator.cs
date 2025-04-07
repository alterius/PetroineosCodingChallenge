using Services;

namespace Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes
{
    public class IntraDayVolumesReportGenerator
    {
        private readonly IPowerService _powerService;

        public IntraDayVolumesReportGenerator(IPowerService powerService)
        {
            _powerService = powerService ?? throw new ArgumentNullException(nameof(powerService));
        }

        // Assumption: For March DST 01:00 is omitted altogether, so Period 3 instead represents 02:00.
        // Assumption: For October DST 01:00 is repeated twice as Periods 3+4, hence are aggregated.
        public async Task<IntraDayVolumesReport> GenerateReport(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            var trades = await _powerService.GetTradesAsync(date.Date);

            var volumesUtc = new SortedList<DateTime, decimal>();
            var dstTransitionOffset = GetDstTransitionOffset(date, timeZoneInfo);

            foreach (var period in trades.SelectMany(t => t.Periods))
            {
                var efaDayOffset = period.Period - 2;
                var dstOffset = period.Period >= 4
                    ? dstTransitionOffset
                    : 0;

                // Convert to UTC for aggregation
                var dateTimeUtc = date.Date.AddHours(efaDayOffset + dstOffset).ToUniversalTime();

                volumesUtc[dateTimeUtc] = volumesUtc.GetValueOrDefault(dateTimeUtc) + (decimal)period.Volume;
            }

            return new IntraDayVolumesReport
            {
                ReportDate = DateOnly.FromDateTime(date),
                Volumes = [.. volumesUtc.Select(v =>
                    new IntraDayVolume
                    {
                        // Convert back to local time for output
                        LocalTime = TimeZoneInfo.ConvertTime(v.Key, timeZoneInfo),
                        Volume = v.Value
                    })]
            };
        }

        private static int GetDstTransitionOffset(DateTime date, TimeZoneInfo timeZone)
        {
            // Start of the current day and the next day
            var startOfDay = date.Date;
            var startOfNextDay = startOfDay.AddDays(1);

            // Get UTC offsets for both days
            var offsetToday = timeZone.GetUtcOffset(startOfDay);
            var offsetTomorrow = timeZone.GetUtcOffset(startOfNextDay);

            // Difference in hours
            // Returns +1, -1, or 0
            return (int)(offsetTomorrow - offsetToday).TotalHours;
        }
    }
}
