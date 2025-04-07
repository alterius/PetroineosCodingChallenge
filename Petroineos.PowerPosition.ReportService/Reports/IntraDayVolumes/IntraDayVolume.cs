using CsvHelper.Configuration.Attributes;

namespace Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes
{
    public record IntraDayVolume
    {
        [Index(0), Name("Local Time"), Format("HH:mm")]
        public DateTime LocalTime { get; init; }
        [Index(1), Name("Volume")]
        public decimal Volume { get; init; }
    }
}
