namespace HCM.Domain.Entities;

public static class AppointmentStatus
{
    public const string Scheduled = "Scheduled";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string NoShow = "NoShow";
    public const string Cancelled = "Cancelled";

    public static readonly string[] AllStatuses = [Scheduled, Confirmed, Completed, NoShow, Cancelled];
}
