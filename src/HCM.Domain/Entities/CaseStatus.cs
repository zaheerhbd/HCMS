namespace HCM.Domain.Entities;

public static class CaseStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string OnHold = "OnHold";
    public const string Closed = "Closed";
    public const string Reopened = "Reopened";

    public static readonly string[] AllStatuses = [Open, InProgress, OnHold, Closed, Reopened];
}
