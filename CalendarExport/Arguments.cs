﻿using System;
using CommandLine;

namespace CalendarExport
{
    public class CalendarExportArguments
    {
        [Option("storage-url", Group = "storage", Default = null, HelpText = "URL to a calendar storage server. HTTPS recommended.")]
        public string StorageUrl { get; set; }

        [Option("storage-directory", Group = "storage", Default = null, HelpText = "Path to a calendar storage directory.")]
        public string StorageDirectory { get; set; }


        [Option("owner-name", Required = false, HelpText = "Unique name to used identify snapshots.")]
        public string OwnerName { get; set; }


        [Option("server-passphrase", Required = false, Default = null, HelpText = "Passphrase used to rw-protect serverside data.")]
        public string ServerPassphrase { get; set; }

        [Option("server-passphrase-file", Required = false, Default = null, HelpText = "Read the server passphrase from the specified file.")]
        public string ServerPassphraseFile { get; set; }

        [Option("encryption-password", Required = false, Default = null, HelpText = "Encrypt the outgoing snapshot with the specified password. Highly recommended.")]
        public string EncryptionPassword { get; set; }

        [Option("encryption-password-file", Required = false, Default = null, HelpText = "Encrypt the outgoing snapshot with the password read from the specified file. Highly recommended.")]
        public string EncryptionPasswordFile { get; set; }


        [Option("full", SetName = "full", Default = false, HelpText = "Create a full calendar snapshot.")]
        public bool FullSnapshot { get; set; }


        [Option("partial", SetName = "partial", Default = false, HelpText = "Create a partial calendar snapshot.")]
        public bool PartialSnapshot { get; set; }

        [Option("try-partial-continue", SetName = "partial", Default = false, HelpText = "Try to capture a partial snapshot since the last one, and default to --partial-start/end if that fails.")]
        public bool PartialSinceLastRun { get; set; }

        [Option("partial-start", SetName = "partial", Default = "now-1d", HelpText = "Partial snapshot \"event modified at\"-since. Allows relative times with \"now-x[wdhm]\".")]
        public string PartialSnapshotStart { get; set; }

        [Option("partial-end", SetName = "partial", Default = "now", HelpText = "Partial snapshot \"event modified at\"-until. Allows relative times with \"now-x[wdhm]\".")]
        public string PartialSnapshotEnd { get; set; }

        [Option("partial-by-edate", SetName = "partial", Default = false, HelpText = "By default, CalendarExport exports partial events filtered by their modification date. This option changes it to event start date.")]
        public bool PartialByEventDate { get; set; }

        [Option("include-recurrences", SetName = "partial", Default = false, HelpText = "Include event recurrences as separate events. Use with --partial-by-cdate")]
        public bool IncludeRecurrences { get; set; }


        [Option("no-sanitize-icals", Required = false, Default = false, HelpText = "Don't strip possibly sensitive info from the iCal files. Use with extreme caution.")]
        public bool DontSanitizeIcals { get; set; }

        [Option("hash-name", Required = false, Default = false, HelpText = "Hash the owner name, instead of sending it in plaintext.")]
        public bool HashName { get; set; }

        [Option("certificate-path", Required = false, Default = null, HelpText = "Path to a self-signed certificate in case the HTTPS server URL uses a custom one.")]
        public string CertificatePath { get; set; }

        [Option("ca-certificate-path", Required = false, Default = null, HelpText = "Path to a self-signed root certificate in case the HTTPS server URL uses a custom one.")]
        public string CACertificatePath { get; set; }

        [Option("single-file", Required = false, Default = false, HelpText = "Combine all events into a single iCal calendar.")]
        public bool SingleFile { get; set; }

        [Option("filter-string", Required = false, Default = null, HelpText = "Extra filter string to be passed to Outlook. Uses Outlook query syntax.")]
        public string FilterString { get; set; }


        public bool Validate()
        {
            // why must the best command line parser be in python 😭

            if (this.StorageUrl != null)
            {
                if (this.OwnerName == null)
                {
                    Console.WriteLine("--owner-name is required when using a storage server.");
                    return false;
                }

                if (this.ServerPassphrase == null && this.ServerPassphraseFile == null)
                {
                    Console.WriteLine("Either --server-passphrase or *-file must be specified when using a storage server!");
                    return false;
                }

                if (this.ServerPassphrase != null && this.ServerPassphraseFile != null)
                {
                    Console.WriteLine("Cannot use --encryption-password and --encryption-password-file together!");
                    return false;
                }
            }

            if (this.EncryptionPassword != null && this.EncryptionPasswordFile != null)
            {
                Console.WriteLine("Cannot use --server-passphrase and --server-passphrase-file together!");
                return false;
            }

            if ((this.FullSnapshot || this.PartialSnapshot) == false)
            {
                Console.WriteLine("Either --full or --partial must be specified!");
                return false;
            }

            if (this.PartialSnapshot && this.IncludeRecurrences && !this.PartialByEventDate)
            {
                Console.WriteLine("--include-recurrences must be used with --partial-by-edate!");
                return false;
            }

            return true;
        }
    }
}
