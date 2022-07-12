using CalendarSyncCommons;
using System;

namespace SyncManager
{
    public class CalendarFetcher : IDisposable
    {
        private readonly ServerInterface ServerInterface;

        public CalendarFetcher(ServerInterface serverInterface)
        {
            this.ServerInterface = serverInterface;
        }
        
        public void Dispose()
        {
            ServerInterface?.Dispose();
        }
    }
}
