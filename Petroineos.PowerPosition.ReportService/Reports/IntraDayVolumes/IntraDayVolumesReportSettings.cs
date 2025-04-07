namespace Petroineos.PowerPosition.ReportService.Reports.IntraDayVolumes
{
    public record IntraDayVolumesReportSettings
    {
        public const string SettingsKey = "IntraDayVolumes";

        public string Schedule { get; init; } = string.Empty;
        public string ExportLocation { get; init; } = string.Empty;
    }
}
