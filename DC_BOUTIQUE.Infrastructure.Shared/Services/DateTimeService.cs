using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Helpers;

namespace DC_BOUTIQUE.Infrastructure.Shared.Services
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime NowUtc => DateTime.UtcNow;

        public DateTime NowLocal => DateTimeHelper.NowDominican;

        public string FormatDominicanDate(DateTime dateTime)
        {
            var local = dateTime.Kind == DateTimeKind.Utc ? DateTimeHelper.ToDominicanTime(dateTime) : dateTime;
            return local.ToString("dd/MM/yyyy");
        }

        public string FormatDominicanDateTime(DateTime dateTime)
        {
            var local = dateTime.Kind == DateTimeKind.Utc ? DateTimeHelper.ToDominicanTime(dateTime) : dateTime;
            return local.ToString("dd/MM/yyyy hh:mm tt");
        }
    }
}
