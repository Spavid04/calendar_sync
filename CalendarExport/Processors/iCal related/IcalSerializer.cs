using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ical.Net;
using Ical.Net.Serialization;

namespace CalendarExport.Processors
{
    public class IcalSerializer : IEnumerable<Stream>
    {
        private readonly IEnumerable<Calendar> Calendars;

        public IcalSerializer(IEnumerable<Calendar> calendars)
        {
            this.Calendars = calendars;
        }

        public IEnumerator<Stream> GetEnumerator()
        {
            var serializer = new CalendarSerializer();

            foreach (var calendar in this.Calendars)
            {
                var ms = new MemoryStream();
                serializer.Serialize(calendar, ms, Encoding.UTF8);
                ms.Seek(0, SeekOrigin.Begin);

                yield return ms;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
