using System;

namespace CalendarStorage
{
    public static class EnvConfig
    {
        public static readonly string DbPath;
        public static readonly int? MaxPartialSnapshots;
        public static readonly int? MaxFullSnapshots;
        public static readonly int? CleanupInterval;
        public static readonly int? MaxSnapshotAge;
        public static readonly bool? DeleteEmptyOwners;
        public static readonly int? MaxPayloadLength;
        public static readonly bool? IncomingBinaryLocking;
        public static readonly string ExporterArchivePath;
        public static readonly string ExporterVersionPath;

        static EnvConfig()
        {
            DbPath = Environment.GetEnvironmentVariable("CALAPI_DB_PATH");
            MaxPartialSnapshots = TryGetInt("CALAPI_MAX_PARTIAL_SS");
            MaxFullSnapshots = TryGetInt("CALAPI_MAX_FULL_SS");
            CleanupInterval = TryGetInt("CALAPI_CLEANUP_INTERVAL");
            MaxSnapshotAge = TryGetInt("CALAPI_MAX_SS_AGE");
            DeleteEmptyOwners = TryGetBool("CALAPI_DELETE_EMPTY_OWNERS");
            MaxPayloadLength = TryGetInt("CALAPI_MAX_PAYLOAD_LENGTH");
            IncomingBinaryLocking = TryGetBool("CALAPI_INCOMING_BINARY_LOCKING");
            ExporterArchivePath = Environment.GetEnvironmentVariable("CALAPI_EXPORTER_ARCHIVE_PATH");
            ExporterVersionPath = Environment.GetEnvironmentVariable("CALAPI_EXPORTER_VERSION_PATH");
        }

        private static int? TryGetInt(string keyName)
        {
            string value = Environment.GetEnvironmentVariable(keyName);
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            int parsed;
            if (int.TryParse(value, out parsed))
            {
                return parsed;
            }
            else
            {
                return null;
            }
        }

        private static bool? TryGetBool(string keyName)
        {
            string value = Environment.GetEnvironmentVariable(keyName);
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return parsed;
            }
            else
            {
                return null;
            }
        }
    }
}
