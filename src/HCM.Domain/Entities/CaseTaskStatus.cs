namespace HCM.Domain.Entities;

public static class CaseTaskStatus
{
    public const string Todo = "Todo";
    public const string InProgress = "InProgress";
    public const string Blocked = "Blocked";
    public const string Done = "Done";
    public const string Cancelled = "Cancelled";

    public static readonly string[] AllStatuses = [Todo, InProgress, Blocked, Done, Cancelled];
}
