using System;

namespace PiClock.Services;

/// <summary>
/// Service chuyển đổi ngày dương lịch sang âm lịch Việt Nam
/// Sử dụng thuật toán tính toán thiên văn chính xác
/// </summary>
public static class LunarCalendarService
{
    private const double PI = Math.PI;

    /// <summary>
    /// Chuyển đổi ngày dương lịch sang âm lịch
    /// </summary>
    public static (int day, int month, int year, bool isLeapMonth) SolarToLunar(DateTime date)
    {
        return SolarToLunar(date.Day, date.Month, date.Year, 7.0); // Múi giờ Việt Nam GMT+7
    }

    private static (int day, int month, int year, bool isLeapMonth) SolarToLunar(int dd, int mm, int yy, double timeZone)
    {
        int dayNumber = JdFromDate(dd, mm, yy);
        int k = (int)((dayNumber - 2415021.076998695) / 29.530588853);

        int monthStart = GetNewMoonDay(k + 1, timeZone);
        if (monthStart > dayNumber)
        {
            monthStart = GetNewMoonDay(k, timeZone);
        }

        int a11 = GetLunarMonth11(yy, timeZone);
        int b11 = a11;
        int lunarYear;

        if (a11 >= monthStart)
        {
            lunarYear = yy;
            a11 = GetLunarMonth11(yy - 1, timeZone);
        }
        else
        {
            lunarYear = yy + 1;
            b11 = GetLunarMonth11(yy + 1, timeZone);
        }

        int lunarDay = dayNumber - monthStart + 1;
        int diff = (int)((monthStart - a11) / 29.0);
        bool isLeapMonth = false;
        int lunarMonth = diff + 11;

        if (b11 - a11 > 365)
        {
            int leapMonthDiff = GetLeapMonthOffset(a11, timeZone);
            if (diff >= leapMonthDiff)
            {
                lunarMonth = diff + 10;
                if (diff == leapMonthDiff)
                {
                    isLeapMonth = true;
                }
            }
        }

        if (lunarMonth > 12)
        {
            lunarMonth = lunarMonth - 12;
        }

        if (lunarMonth >= 11 && diff < 4)
        {
            lunarYear -= 1;
        }

        return (lunarDay, lunarMonth, lunarYear, isLeapMonth);
    }

    /// <summary>
    /// Tính số ngày Julius từ ngày dương lịch
    /// </summary>
    private static int JdFromDate(int dd, int mm, int yy)
    {
        int a = (14 - mm) / 12;
        int y = yy + 4800 - a;
        int m = mm + 12 * a - 3;
        int jd = dd + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
        if (jd < 2299161)
        {
            jd = dd + (153 * m + 2) / 5 + 365 * y + y / 4 - 32083;
        }
        return jd;
    }

    /// <summary>
    /// Tính ngày Sóc (New Moon)
    /// </summary>
    private static int GetNewMoonDay(int k, double timeZone)
    {
        double T = k / 1236.85;
        double T2 = T * T;
        double T3 = T2 * T;
        double dr = PI / 180.0;

        double Jd1 = 2415020.75933 + 29.53058868 * k + 0.0001178 * T2 - 0.000000155 * T3;
        Jd1 = Jd1 + 0.00033 * Math.Sin((166.56 + 132.87 * T - 0.009173 * T2) * dr);

        double M = 359.2242 + 29.10535608 * k - 0.0000333 * T2 - 0.00000347 * T3;
        double Mpr = 306.0253 + 385.81691806 * k + 0.0107306 * T2 + 0.00001236 * T3;
        double F = 21.2964 + 390.67050646 * k - 0.0016528 * T2 - 0.00000239 * T3;

        double C1 = (0.1734 - 0.000393 * T) * Math.Sin(M * dr) + 0.0021 * Math.Sin(2 * dr * M);
        C1 = C1 - 0.4068 * Math.Sin(Mpr * dr) + 0.0161 * Math.Sin(dr * 2 * Mpr);
        C1 = C1 - 0.0004 * Math.Sin(dr * 3 * Mpr);
        C1 = C1 + 0.0104 * Math.Sin(dr * 2 * F) - 0.0051 * Math.Sin(dr * (M + Mpr));
        C1 = C1 - 0.0074 * Math.Sin(dr * (M - Mpr)) + 0.0004 * Math.Sin(dr * (2 * F + M));
        C1 = C1 - 0.0004 * Math.Sin(dr * (2 * F - M)) - 0.0006 * Math.Sin(dr * (2 * F + Mpr));
        C1 = C1 + 0.0010 * Math.Sin(dr * (2 * F - Mpr)) + 0.0005 * Math.Sin(dr * (2 * Mpr + M));

        double deltat;
        if (T < -11)
        {
            deltat = 0.001 + 0.000839 * T + 0.0002261 * T2 - 0.00000845 * T3 - 0.000000081 * T * T3;
        }
        else
        {
            deltat = -0.000278 + 0.000265 * T + 0.000262 * T2;
        }

        double JdNew = Jd1 + C1 - deltat;
        return (int)(JdNew + 0.5 + timeZone / 24.0);
    }

    /// <summary>
    /// Tính tọa độ mặt trời
    /// </summary>
    private static double GetSunLongitude(int jdn, double timeZone)
    {
        double T = (jdn - 0.5 - timeZone / 24.0 - 2451545.0) / 36525.0;
        double T2 = T * T;
        double dr = PI / 180.0;

        double M = 357.52910 + 35999.05030 * T - 0.0001559 * T2 - 0.00000048 * T * T2;
        double L0 = 280.46645 + 36000.76983 * T + 0.0003032 * T2;
        double DL = (1.914600 - 0.004817 * T - 0.000014 * T2) * Math.Sin(dr * M);
        DL = DL + (0.019993 - 0.000101 * T) * Math.Sin(dr * 2 * M) + 0.000290 * Math.Sin(dr * 3 * M);

        double L = L0 + DL;
        L = L * dr;
        L = L - PI * 2 * (int)(L / (PI * 2));

        return (int)(L / PI * 6.0);
    }

    /// <summary>
    /// Tìm ngày bắt đầu tháng 11 âm lịch
    /// </summary>
    private static int GetLunarMonth11(int yy, double timeZone)
    {
        int off = JdFromDate(31, 12, yy) - 2415021;
        int k = (int)(off / 29.530588853);
        int nm = GetNewMoonDay(k, timeZone);
        int sunLong = (int)GetSunLongitude(nm, timeZone);

        if (sunLong >= 9)
        {
            nm = GetNewMoonDay(k - 1, timeZone);
        }

        return nm;
    }

    /// <summary>
    /// Tìm offset của tháng nhuận
    /// </summary>
    private static int GetLeapMonthOffset(int a11, double timeZone)
    {
        int k = (int)((a11 - 2415021.076998695) / 29.530588853 + 0.5);
        int last;
        int i = 1;
        int arc = (int)GetSunLongitude(GetNewMoonDay(k + i, timeZone), timeZone);

        do
        {
            last = arc;
            i++;
            arc = (int)GetSunLongitude(GetNewMoonDay(k + i, timeZone), timeZone);
        } while (arc != last && i < 14);

        return i - 1;
    }

    /// <summary>
    /// Lấy chuỗi hiển thị ngày âm lịch ngắn gọn
    /// </summary>
    public static string GetLunarDateShort(DateTime date)
    {
        var (day, month, year, isLeap) = SolarToLunar(date);
        string leapMark = isLeap ? "*" : "";
        return $"{day}/{month}{leapMark} ÂL";
    }

    /// <summary>
    /// Lấy chuỗi hiển thị ngày âm lịch đầy đủ
    /// </summary>
    public static string GetLunarDateFull(DateTime date)
    {
        var (day, month, year, isLeap) = SolarToLunar(date);
        string[] monthNames = { "Giêng", "Hai", "Ba", "Tư", "Năm", "Sáu", "Bảy", "Tám", "Chín", "Mười", "M.Một", "Chạp" };
        string monthName = isLeap ? $"Nhuận {monthNames[month - 1]}" : monthNames[month - 1];
        return $"Ngày {day} tháng {monthName}";
    }

    /// <summary>
    /// Lấy năm Can Chi
    /// </summary>
    public static string GetCanChiYear(DateTime date)
    {
        var (_, _, lunarYear, _) = SolarToLunar(date);
        string[] canNames = { "Giáp", "Ất", "Bính", "Đinh", "Mậu", "Kỷ", "Canh", "Tân", "Nhâm", "Quý" };
        string[] chiNames = { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };

        int canIndex = (lunarYear + 6) % 10;
        int chiIndex = (lunarYear + 8) % 12;
        return $"{canNames[canIndex]} {chiNames[chiIndex]}";
    }
}
