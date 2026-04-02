namespace ScreeningLogicServiceApp.Models;

public sealed class IncomingOrderDashboardMetrics
{
    public int ApplicationProcessedCount { get; init; }
    public DateTime? Since { get; init; }
    public decimal HitRatioPercent { get; init; }
    public TimeSpan? AverageProcessTime { get; init; }
    public TimeSpan? AverageTurnaround { get; init; }
}
