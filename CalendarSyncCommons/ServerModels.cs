namespace CalendarSyncCommons
{
    public static class ServerModels
    {
        public class AvailableSnapshot
        {
            public int Id { get; set; }
            public string Timestamp { get; set; }
            public string SnapshotType { get; set; }
            public string EventModifiedAt_IntervalStart { get; set; }
            public string EventModifiedAt_IntervalEnd { get; set; }
        }
    }
}
