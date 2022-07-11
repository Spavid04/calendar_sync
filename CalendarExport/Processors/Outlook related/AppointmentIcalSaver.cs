using System.Collections;
using System.Collections.Generic;
using System.IO;

using Outlook = Microsoft.Office.Interop.Outlook;

namespace CalendarExport.Processors
{
    public class AppointmentIcalSaver : IEnumerable<string>
    {
        private readonly IEnumerable<Outlook.AppointmentItem> Items;
        private readonly string Directory;

        public AppointmentIcalSaver(IEnumerable<Outlook.AppointmentItem> items, string directory)
        {
            this.Items = items;
            this.Directory = directory;
        }

        public IEnumerator<string> GetEnumerator()
        {
            int i = 0;
            foreach (var item in this.Items)
            {
                string path = Path.Combine(this.Directory, $"{i:D5}.ical");
                item.SaveAs(path, Outlook.OlSaveAsType.olICal);
                yield return path;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
