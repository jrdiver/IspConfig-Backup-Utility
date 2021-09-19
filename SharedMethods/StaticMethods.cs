using System;
using System.Globalization;

namespace SharedMethods
{
    public static class StaticMethods
    {
        public static DateTime Epoch2SDate(long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
        }

        public static int GetWeekNumber(DateTime date)
        {
            Calendar cal = new CultureInfo("en-US").Calendar;
            return cal.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
        }
    }
}
