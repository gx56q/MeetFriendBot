using System.Text;
using Domain;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Infrastructure.Calendar;

public class CalendarGenerator
{
    public static byte[] GenerateEventCalendar(Event data)
    {
        var calendarEvent = new CalendarEvent
        {
            Summary = data.Name,
            Description = data.Description,
            Location = data.Location?.Loc ?? "",
            DtStart = new CalDateTime(data.Date!.Value)
        };

        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(calendarEvent);
        
        
        var serializer = new CalendarSerializer();
        var calendarString = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(calendarString);
    }
}