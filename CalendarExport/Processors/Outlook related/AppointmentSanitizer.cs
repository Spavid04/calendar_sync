using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

using Outlook = Microsoft.Office.Interop.Outlook;

namespace CalendarExport.Processors
{
    public class AppointmentSanitizer : IEnumerable<Calendar>
    {
        private readonly IEnumerable<Outlook.AppointmentItem> Appointments;
        private readonly bool AddRecurrenceRules;

        public AppointmentSanitizer(IEnumerable<Outlook.AppointmentItem> appointments, bool addRecurrenceRules = true)
        {
            this.Appointments = appointments;
            this.AddRecurrenceRules = addRecurrenceRules;
        }

        public IEnumerator<Calendar> GetEnumerator()
        {
            foreach (var appointment in this.Appointments)
            {
                yield return AppointmentToCalendar(appointment);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Calendar? AppointmentToCalendar(Outlook.AppointmentItem appointment)
        {
            var calendar = new Calendar();
            var evt = new CalendarEvent();

            evt.Uid = appointment.GlobalAppointmentID;

            evt.DtStart = new CalDateTime(appointment.Start.ToUniversalTime());
            evt.DtEnd = new CalDateTime(appointment.End.ToUniversalTime());
            evt.IsAllDay = appointment.AllDayEvent;

            evt.Created = new CalDateTime(appointment.CreationTime.ToUniversalTime());
            evt.LastModified = new CalDateTime(appointment.LastModificationTime.ToUniversalTime());
            evt.Sequence = appointment.LastModificationTime.ToUniversalTime().ToEpochSeconds();

            if (appointment.RecurrenceState == Outlook.OlRecurrenceState.olApptMaster)
            {
                evt.RecurrenceRules = ConvertRecurrencePatterns(appointment.GetRecurrencePattern());
            }

            if (appointment.MeetingStatus == Outlook.OlMeetingStatus.olMeetingCanceled ||
                appointment.MeetingStatus == Outlook.OlMeetingStatus.olMeetingReceivedAndCanceled)
            {
                evt.Status = "CANCELLED";
            }
            else if (appointment.ResponseStatus == Outlook.OlResponseStatus.olResponseOrganized ||
                     appointment.ResponseStatus == Outlook.OlResponseStatus.olResponseAccepted)
            {
                evt.Status = "CONFIRMED";
            }
            else
            {
                evt.Status = "TENTATIVE";
            }

            evt.Summary = appointment.Subject.ToQuotedPrintable();
            evt.Description = RemoveLinks(appointment.Body).ToQuotedPrintable() ?? ""; // some ical parsers don't like this being null
            evt.Location = appointment.Location.ToQuotedPrintable();
            if (appointment.Categories != null)
            {
                evt.Categories = appointment.Categories.Split(',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.ToQuotedPrintable()).ToList();
            }

            evt.Organizer = new Organizer()
            {
                CommonName = appointment.Organizer,
                Encoding = "UTF-8"
            };

            evt.Attendees = new List<Attendee>();
            string requiredRecipients = appointment.RequiredAttendees;
            foreach (Outlook.Recipient recipient in appointment.Recipients)
            {
                evt.Attendees.Add(new Attendee()
                {
                    CommonName = recipient.Name,
                    Role = requiredRecipients.Contains(recipient.Name) ? "REQ-PARTICIPANT" : "OPT-PARTICIPANT",
                    Rsvp = false,
                    Encoding = "UTF-8"
                });
            }

            calendar.Events.Add(evt);
            return calendar;
        }

        private static string RemoveLinks(string text)
        {
            if (text == null)
            {
                return null;
            }

            const string urlPattern =
                @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
            Regex urlRegex = new Regex(urlPattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex urlTagRegex = new Regex($@"<\s*{urlPattern}\s*>",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return urlRegex.Replace(urlTagRegex.Replace(text, ""), "");
        }

        private static IList<RecurrencePattern> ConvertRecurrencePatterns(Outlook.RecurrencePattern oRecurrence)
        {
            var pattern = new RecurrencePattern()
            {
                FirstDayOfWeek = DayOfWeek.Monday
            };

            switch (oRecurrence.RecurrenceType)
            {
                default:
                    throw new ArgumentOutOfRangeException();

                case Outlook.OlRecurrenceType.olRecursDaily:
                    pattern.Frequency = FrequencyType.Daily;
                    pattern.Interval = oRecurrence.Interval;
                    break;
                case Outlook.OlRecurrenceType.olRecursWeekly:
                    pattern.Frequency = FrequencyType.Weekly;
                    pattern.ByDay = GetDaysOfWeekFromMask(oRecurrence.DayOfWeekMask)
                        .Select(x => new WeekDay(x)).ToList();
                    pattern.Interval = oRecurrence.Interval;
                    break;
                case Outlook.OlRecurrenceType.olRecursMonthly:
                    pattern.Frequency = FrequencyType.Monthly;
                    pattern.ByMonthDay = new List<int>() { oRecurrence.DayOfMonth };
                    pattern.Interval = oRecurrence.Interval;
                    break;
                case Outlook.OlRecurrenceType.olRecursMonthNth:
                    pattern.Frequency = FrequencyType.Monthly;
                    var fo = GetEveryNthDay(oRecurrence.Instance);
                    pattern.ByDay = GetDaysOfWeekFromMask(oRecurrence.DayOfWeekMask)
                        .Select(x => new WeekDay(x, fo)).ToList();
                    pattern.Interval = oRecurrence.Interval;
                    break;
                case Outlook.OlRecurrenceType.olRecursYearly:
                    pattern.Frequency = FrequencyType.Yearly;
                    pattern.ByMonthDay = new List<int>() { oRecurrence.DayOfMonth };
                    pattern.ByMonth = new List<int>() { oRecurrence.MonthOfYear };
                    pattern.Interval = oRecurrence.Interval;
                    break;
                case Outlook.OlRecurrenceType.olRecursYearNth:
                    pattern.Frequency = FrequencyType.Yearly;
                    var fo2 = GetEveryNthDay(oRecurrence.Instance);
                    pattern.ByDay = GetDaysOfWeekFromMask(oRecurrence.DayOfWeekMask)
                        .Select(x => new WeekDay(x, fo2)).ToList();
                    pattern.ByMonth = new List<int>() { oRecurrence.MonthOfYear };
                    pattern.Interval = oRecurrence.Interval / 12; // for some reason...
                    break;
            }

            if (!oRecurrence.NoEndDate)
            {
                if (oRecurrence.Occurrences > 0)
                {
                    pattern.Count = oRecurrence.Occurrences;
                }
                else
                {
                    pattern.Until = (oRecurrence.PatternEndDate.Date + oRecurrence.EndTime.TimeOfDay).ToUniversalTime();
                }
            }

            return new List<RecurrencePattern>() { pattern };
        }

        private static List<DayOfWeek> GetDaysOfWeekFromMask(Outlook.OlDaysOfWeek mask)
        {
            var days = new List<DayOfWeek>();

            if ((mask & Outlook.OlDaysOfWeek.olMonday) != 0)
            {
                days.Add(DayOfWeek.Monday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olTuesday) != 0)
            {
                days.Add(DayOfWeek.Tuesday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olWednesday) != 0)
            {
                days.Add(DayOfWeek.Wednesday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olThursday) != 0)
            {
                days.Add(DayOfWeek.Thursday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olFriday) != 0)
            {
                days.Add(DayOfWeek.Friday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olSaturday) != 0)
            {
                days.Add(DayOfWeek.Saturday);
            }
            if ((mask & Outlook.OlDaysOfWeek.olSunday) != 0)
            {
                days.Add(DayOfWeek.Sunday);
            }

            return days;
        }

        private static FrequencyOccurrence GetEveryNthDay(int instance)
        {
            switch (instance)
            {
                default:
                    // outlook doesn't support more than these
                    throw new ArgumentOutOfRangeException();

                case 1:
                    return FrequencyOccurrence.First;
                case 2:
                    return FrequencyOccurrence.Second;
                case 3:
                    return FrequencyOccurrence.Third;
                case 4:
                    return FrequencyOccurrence.Fourth;
                case 5:
                    return FrequencyOccurrence.Last;
            }
        }
    }
}
