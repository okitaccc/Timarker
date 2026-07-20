using System.Globalization;

namespace Timarker;

public static class LunarDate
{
    private static readonly string[] Months = ["正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月"];
    private static readonly string[] Days = ["初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十", "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十", "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"];

    public static string Text(DateTime date)
    {
        try
        {
            var calendar = new ChineseLunisolarCalendar();
            var month = calendar.GetMonth(date);
            var leap = calendar.GetLeapMonth(calendar.GetYear(date));
            if (leap > 0 && month >= leap)
            {
                month--;
            }
            var day = calendar.GetDayOfMonth(date);
            return day == 1 ? Months[Math.Clamp(month - 1, 0, 11)] : Days[Math.Clamp(day - 1, 0, 29)];
        }
        catch
        {
            return "";
        }
    }

    public static string FullText(DateTime date)
    {
        try
        {
            var calendar = new ChineseLunisolarCalendar();
            var month = calendar.GetMonth(date);
            var leap = calendar.GetLeapMonth(calendar.GetYear(date));
            var leapPrefix = leap > 0 && month == leap ? "闰" : "";
            if (leap > 0 && month >= leap) month--;
            var day = calendar.GetDayOfMonth(date);
            return $"{leapPrefix}{Months[Math.Clamp(month - 1, 0, 11)]}{Days[Math.Clamp(day - 1, 0, 29)]}";
        }
        catch
        {
            return "";
        }
    }
}
