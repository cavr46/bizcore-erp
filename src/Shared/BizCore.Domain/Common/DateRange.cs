using System;
using System.Collections.Generic;

namespace BizCore.Domain.Common;

public class DateRange : ValueObject
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    private DateRange() { }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be after or equal to start date");

        StartDate = startDate.Date;
        EndDate = endDate.Date;
    }

    public static DateRange Today() => new(DateTime.Today, DateTime.Today);
    public static DateRange ThisWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var startOfWeek = today.AddDays(-dayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return new(startOfWeek, endOfWeek);
    }

    public static DateRange ThisMonth()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return new(startOfMonth, endOfMonth);
    }

    public static DateRange ThisQuarter()
    {
        var today = DateTime.Today;
        var quarter = (today.Month - 1) / 3;
        var startMonth = quarter * 3 + 1;
        var startOfQuarter = new DateTime(today.Year, startMonth, 1);
        var endOfQuarter = startOfQuarter.AddMonths(3).AddDays(-1);
        return new(startOfQuarter, endOfQuarter);
    }

    public static DateRange ThisYear()
    {
        var year = DateTime.Today.Year;
        return new(new DateTime(year, 1, 1), new DateTime(year, 12, 31));
    }

    public static DateRange LastDays(int days)
    {
        var today = DateTime.Today;
        return new(today.AddDays(-days), today);
    }

    public int Days => (EndDate - StartDate).Days + 1;
    public int Months => ((EndDate.Year - StartDate.Year) * 12) + EndDate.Month - StartDate.Month;

    public bool Contains(DateTime date) => date.Date >= StartDate && date.Date <= EndDate;
    public bool Overlaps(DateRange other) => StartDate <= other.EndDate && EndDate >= other.StartDate;

    public DateRange Extend(int days) => new(StartDate, EndDate.AddDays(days));
    public DateRange Shift(int days) => new(StartDate.AddDays(days), EndDate.AddDays(days));

    public IEnumerable<DateTime> GetDates()
    {
        for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            yield return date;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}