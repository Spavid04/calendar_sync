using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ical.Net;

namespace CalendarExport.Processors
{
    public class IcalDeserializer : IEnumerable<Calendar>
    {
        private readonly IEnumerable<Stream> IcalContents;

        public IcalDeserializer(IEnumerable<Stream> icalContents)
        {
            this.IcalContents = icalContents;
        }

        public IEnumerator<Calendar> GetEnumerator()
        {
            foreach (var content in this.IcalContents)
            {
                yield return Calendar.Load(content);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
