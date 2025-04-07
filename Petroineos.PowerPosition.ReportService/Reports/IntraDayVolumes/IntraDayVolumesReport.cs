namespace Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes
{
    public record IntraDayVolumesReport
    {
        public DateOnly ReportDate { get; init; }
        public IReadOnlyCollection<IntraDayVolume> Volumes { get; init; } = [];
    }
}
