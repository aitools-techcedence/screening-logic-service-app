namespace ScreeningLogicServiceApp.Models;

public sealed class IncomingOrderSearchResult
{
    public int WorkId { get; set; }
    public string? OrderNumber { get; set; }
    public string? NameFirst { get; set; }
    public string? NameLast { get; set; }
    public string? Dob { get; set; }
    public string? Ssn { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailedSummaryReport { get; set; }
    public bool CanShowErrorDetails => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? DobDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Dob))
            {
                return Dob;
            }

            if (Dob.Length == 8
                && int.TryParse(Dob[..4], out var year)
                && int.TryParse(Dob.Substring(4, 2), out var month)
                && int.TryParse(Dob.Substring(6, 2), out var day)
                && month is >= 1 and <= 12
                && day is >= 1 and <= 31)
            {
                return $"{month:00}/{day:00}/{year:0000}";
            }

            return Dob;
        }
    }
}
