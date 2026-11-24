namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IDateTimeService
    {
        DateTime NowUtc { get; }
        DateTime NowLocal { get; }
        string FormatDominicanDate(DateTime dateTime);
        string FormatDominicanDateTime(DateTime dateTime);
    }
}
