using System;
using System.Linq;
using Ical.Net.DataTypes;
using Ical.Net.Interfaces.DataTypes;
using NodaTime;

namespace Ical.Net.Utility
{
    public class DateUtil
    {
        private static System.Globalization.Calendar _calendar;

        static DateUtil()
        {
            _calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        }

        public static IDateTime StartOfDay(IDateTime dt)
        {
            return dt.
                AddHours(-dt.Hour).
                AddMinutes(-dt.Minute).
                AddSeconds(-dt.Second);
        }

        public static IDateTime EndOfDay(IDateTime dt)
        {
            return StartOfDay(dt).AddDays(1).AddTicks(-1);
        }     

        public static DateTime GetSimpleDateTimeData(IDateTime dt)
        {
            return DateTime.SpecifyKind(dt.Value, dt.IsUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        public static DateTime SimpleDateTimeToMatch(IDateTime dt, IDateTime toMatch)
        {
            if (toMatch.IsUniversalTime && dt.IsUniversalTime)
                return dt.Value;
            else if (toMatch.IsUniversalTime)
                return dt.Value.ToUniversalTime();
            else if (dt.IsUniversalTime)
                return dt.Value.ToLocalTime();
            else
                return dt.Value;
        }

        public static IDateTime MatchTimeZone(IDateTime dt1, IDateTime dt2)
        {

            // Associate the date/time with the first.
            var copy = dt2.Copy<IDateTime>();
            copy.AssociateWith(dt1);

            // If the dt1 time does not occur in the same time zone as the
            // dt2 time, then let's convert it so they can be used in the
            // same context (i.e. evaluation).
            if (dt1.TzId != null)
            {
                if (!string.Equals(dt1.TzId, copy.TzId))
                    return (dt1.TimeZoneObservance != null) ? copy.ToTimeZone(dt1.TimeZoneObservance.Value) : copy.ToTimeZone(dt1.TzId);
                else return copy;
            }
            else if (dt1.IsUniversalTime)
            {
                // The first date/time is in UTC time, convert!
                return new CalDateTime(copy.AsUtc);
            }
            else
            {
                // The first date/time is in local time, convert!
                return new CalDateTime(copy.AsSystemLocal);
            }
        }

        public static DateTime AddWeeks(DateTime dt, int interval, DayOfWeek firstDayOfWeek)
        {
            // NOTE: fixes WeeklyUntilWkst2() eval.
            // NOTE: simplified the execution of this - fixes bug #3119920 - missing weekly occurences also
            dt = dt.AddDays(interval * 7);
            while (dt.DayOfWeek != firstDayOfWeek)
                dt = dt.AddDays(-1);

            return dt;
        }

        public static DateTime FirstDayOfWeek(DateTime dt, DayOfWeek firstDayOfWeek, out int offset)
        {
            offset = 0;
            while (dt.DayOfWeek != firstDayOfWeek)
            {
                dt = dt.AddDays(-1);
                offset++;
            }
            return dt;
        }

        public static readonly DateTimeZone LocalDateTimeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();

        public static DateTimeZone GetZone(string tzId)
        {
            if (string.IsNullOrWhiteSpace(tzId))
            {
                return LocalDateTimeZone;
            }

            var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tzId);
            if (zone != null)
            {
                return zone;
            }

            zone = DateTimeZoneProviders.Bcl.GetZoneOrNull(tzId);
            if (zone != null)
            {
                return zone;
            }

            zone = DateTimeZoneProviders.Serialization.GetZoneOrNull(tzId);
            if (zone != null)
            {
                return zone;
            }

            var newTzId = tzId.Replace("-", "/");
            zone = DateTimeZoneProviders.Serialization.GetZoneOrNull(newTzId);
            if (zone != null)
            {
                return zone;
            }

            foreach (var providerId in DateTimeZoneProviders.Tzdb.Ids.Where(tzId.Contains))
            {
                return DateTimeZoneProviders.Tzdb.GetZoneOrNull(providerId);
            }

            foreach (var providerId in DateTimeZoneProviders.Bcl.Ids.Where(tzId.Contains))
            {
                return DateTimeZoneProviders.Bcl.GetZoneOrNull(providerId);
            }

            foreach (var providerId in DateTimeZoneProviders.Serialization.Ids.Where(tzId.Contains))
            {
                return DateTimeZoneProviders.Serialization.GetZoneOrNull(providerId);
            }

            return LocalDateTimeZone;
        }

        public static ZonedDateTime AddYears(ZonedDateTime zonedDateTime, int years)
        {
            var futureDate = zonedDateTime.Date.PlusYears(years);
            var futureLocalDateTime = new LocalDateTime(futureDate.Year, futureDate.Month, futureDate.Day, zonedDateTime.Hour, zonedDateTime.Minute,
                zonedDateTime.Second);
            var zonedFutureDate = new ZonedDateTime(futureLocalDateTime, zonedDateTime.Zone, zonedDateTime.Offset);
            return zonedFutureDate;
        }

        public static ZonedDateTime AddMonths(ZonedDateTime zonedDateTime, int months)
        {
            var futureDate = zonedDateTime.Date.PlusMonths(months);
            var futureLocalDateTime = new LocalDateTime(futureDate.Year, futureDate.Month, futureDate.Day, zonedDateTime.Hour, zonedDateTime.Minute,
                zonedDateTime.Second);
            var zonedFutureDate = new ZonedDateTime(futureLocalDateTime, zonedDateTime.Zone, zonedDateTime.Offset);
            return zonedFutureDate;
        }

        public static ZonedDateTime ToZonedDateTimeLeniently(DateTime dateTime, string tzId)
        {
            var zone = GetZone(tzId);
            var localDt = LocalDateTime.FromDateTime(dateTime);//19:00 UTC
            var lenientZonedDateTime = localDt.InZoneLeniently(zone).WithZone(zone);//15:00 Eastern
            return lenientZonedDateTime;
        }

        public static ZonedDateTime FromTimeZoneToTimeZone(DateTime dateTime, string fromZoneId, string toZoneId)
            => FromTimeZoneToTimeZone(dateTime, GetZone(fromZoneId), GetZone(toZoneId));

        public static ZonedDateTime FromTimeZoneToTimeZone(DateTime dateTime, DateTimeZone fromZone, DateTimeZone toZone)
        {
            var oldZone = LocalDateTime.FromDateTime(dateTime).InZoneLeniently(fromZone);
            var newZone = oldZone.WithZone(toZone);
            return newZone;
        }

        public static bool IsSerializationTimeZone(DateTimeZone zone) => DateTimeZoneProviders.Serialization.GetZoneOrNull(zone.Id) != null;
    }
}
