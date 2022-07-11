using System;
using CommandLine;

namespace CalendarExport
{
    public class CalendarExportArguments
    {
        [Option("storage-url", Group = "storage", HelpText = "URL to a calendar storage server. HTTPS recommended.")]
        public string StorageUrl { get; set; }

        [Option("storage-dir", Group = "storage", HelpText = "Path to a calendar storage directory.")]
        public string StorageDirectory { get; set; }


        [Option("owner-name", Required = true, HelpText = "Unique name to used identify snapshots.")]
        public string OwnerName { get; set; }


        [Option("server-passphrase", Group = "serverpassphrase", Default = null, HelpText = "Passphrase used to rw-protect serverside data.")]
        public string ServerPassphrase { get; set; }

        [Option("server-passphrase-file", Group = "serverpassphrase", Default = null, HelpText = "Read the server passphrase from the specified file.")]
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


        [Option("no-sanitize-icals", Required = false, Default = false, HelpText = "Don't strip possibly sensitive info from the iCal files. Use with extreme caution.")]
        public bool DontSanitizeIcals { get; set; }

        [Option("hash-name", Required = false, Default = false, HelpText = "Hash the owner name, instead of sending it in plaintext.")]
        public bool HashName { get; set; }

        [Option("certificate-path", Required = false, Default = null, HelpText = "Path to a self-signed certificate in case the HTTPS server URL uses a custom one.")]
        public string CertificatePath { get; set; }


        public bool Validate()
        {
            // why must the best command line parser be in python 😭
            
            if (this.ServerPassphrase != null && this.ServerPassphraseFile != null)
            {
                Console.WriteLine("Cannot use --encryption-password and --encryption-password-file together!");
                return false;
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

            return true;
        }
    }
}
