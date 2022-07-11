using Ical.Net;
using System.Collections.Generic;
using System.IO;

using Outlook = Microsoft.Office.Interop.Outlook;

namespace CalendarExport.Processors
{
    public static class Glue
    {
        public static AppointmentSanitizer SanitizeAppointments(this IEnumerable<Outlook.AppointmentItem> appointments)
        {
            return new AppointmentSanitizer(appointments);
        }

        public static AppointmentIcalSaver DumpAsIcals(this IEnumerable<Outlook.AppointmentItem> items, string directory)
        {
            return new AppointmentIcalSaver(items, directory);
        }

        public static RawFilesReader ReadFiles(this IEnumerable<string> files)
        {
            return new RawFilesReader(files, true, true);
        }

        public static IcalDeserializer DeserializeIcals(this IEnumerable<Stream> streams)
        {
            return new IcalDeserializer(streams);
        }
        
        public static IcalSerializer SerializeIcals(this IEnumerable<Calendar> calendars)
        {
            return new IcalSerializer(calendars);
        }
    }
}
