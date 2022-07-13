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

        private readonly DateTime From;
        private readonly DateTime To;
        private readonly FilterBy FilterType;
        private readonly bool IncludeRecurrences;

        public DisposableAppointmentFetcher(DateTime from, DateTime to, FilterBy filterType = FilterBy.ModifiedDate, bool includeRecurrences = false)
        {
            this.Application = new Outlook.Application();
            this.MapiNamespace = this.Application.GetNamespace("MAPI");
            this.CalendarFolder = this.MapiNamespace.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar);

            this.From = from.ToLocalTime();
            this.To = to.ToLocalTime();
            this.FilterType = filterType;
            this.IncludeRecurrences = includeRecurrences;
        }

        public IEnumerator<Outlook.AppointmentItem> GetEnumerator()
        {
            Outlook.Items items = this.CalendarFolder.Items;
            this.ApplyFilters(items);

            foreach (Outlook.AppointmentItem item in items)
            {
                yield return item;
            }
        }

        private void ApplyFilters(Outlook.Items items)
        {
            items.IncludeRecurrences = this.IncludeRecurrences;

            string field = this.FilterType switch
            {
                FilterBy.ModifiedDate => nameof(Outlook.AppointmentItem.LastModificationTime),
                FilterBy.EventDate => nameof(Outlook.AppointmentItem.Start),
                _ => throw new ArgumentOutOfRangeException()
            };

            string from = this.From.ToString("O");
            string to = this.To.ToString("O");
            string query = $"[{field}] >= \"{from}\" and [{field}] <= \"{to}\"";
            items.Restrict(query);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            this.MapiNamespace.Logoff();
        }

        public enum FilterBy
        {
            ModifiedDate,
            EventDate
        }
    }
}
