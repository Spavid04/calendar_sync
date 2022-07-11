using System;
using System.Collections;
using System.Collections.Generic;

using Outlook = Microsoft.Office.Interop.Outlook;

namespace CalendarExport.Processors
{
    public class DisposableAppointmentFetcher : IDisposable, IEnumerable<Outlook.AppointmentItem>
    {
        private readonly Outlook.Application Application;
        private readonly Outlook.NameSpace MapiNamespace;
        private readonly Outlook.MAPIFolder CalendarFolder;

        private readonly DateTime ModifiedSince;
        private readonly DateTime ModifiedUntil;
        private readonly bool IncludeRecurrences;

        public DisposableAppointmentFetcher(DateTime modifiedSince, DateTime modifiedUntil, bool includeRecurrences = false)
        {
            this.Application = new Outlook.Application();
            this.MapiNamespace = this.Application.GetNamespace("MAPI");
            this.CalendarFolder = this.MapiNamespace.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar);

            this.ModifiedSince = modifiedSince.ToLocalTime();
            this.ModifiedUntil = modifiedUntil.ToLocalTime();
            this.IncludeRecurrences = includeRecurrences;
        }

        public IEnumerator<Outlook.AppointmentItem> GetEnumerator()
        {
            Outlook.Items items = this.CalendarFolder.Items;
            items.IncludeRecurrences = this.IncludeRecurrences;

            foreach (Outlook.AppointmentItem item in items)
            {
                if (item.LastModificationTime >= this.ModifiedSince && item.LastModificationTime <= this.ModifiedUntil)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            this.MapiNamespace.Logoff();
        }
    }
}
