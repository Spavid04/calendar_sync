﻿using CalendarExport.Processors;
using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using CalendarSyncCommons;
using Ical.Net;

namespace CalendarExport
{
    public class Exporter
    {
        private CalendarExportArguments Arguments;

        public Exporter(CalendarExportArguments arguments)
        {
            this.Arguments = arguments;

            if (this.Arguments.StorageDirectory != null)
            {
                Directory.CreateDirectory(this.Arguments.StorageDirectory);
            }
        }

        public bool DoMagic()
        {
            DateTime dtStart, dtEnd;

            if (this.Arguments.FullSnapshot)
            {
                dtStart = DateTime.MinValue;
                dtEnd = DateTime.MaxValue;
            }
            else
            {
                dtStart =
                    (this.Arguments.PartialSinceLastRun ? GetLastRun() : null)
                    ?? TryGetRelativeOrAbsoluteDatetime(this.Arguments.PartialSnapshotStart)
                    ?? DateTime.MinValue;
                dtEnd = TryGetRelativeOrAbsoluteDatetime(this.Arguments.PartialSnapshotEnd) ?? DateTime.MaxValue;
            }

            DateTime fetchedAt = DateTime.Now;
            using var appointmentCollection = new DisposableAppointmentFetcher(dtStart, dtEnd,
                this.Arguments.PartialByEventDate
                    ? DisposableAppointmentFetcher.FilterBy.EventDate
                    : DisposableAppointmentFetcher.FilterBy.ModifiedDate,
                this.Arguments.IncludeRecurrences,
                this.Arguments.FilterString);

            int progress = 0;
            int count = 0;
            if (appointmentCollection.TryGetCount(out count))
            {
                Console.WriteLine($"Fetching {count} events...");
            }
            else
            {
                Console.WriteLine("Fetching an unknown number of events...");
            }
            appointmentCollection.Progress += (item =>
            {
                progress++;

                string sdt = item.Start.ToShortDateString();
                int titleLength = item.Subject.Length;
                string title = item.Subject.Substring(0, Math.Min(titleLength, 30));
                if (titleLength > 30)
                {
                    title += "...";
                }

                Console.WriteLine($"{progress}/{count}\t{sdt}\t{title}");
            });

            IEnumerable<Stream> icalDataStreams;
            
            if (this.Arguments.DontSanitizeIcals)
            {
                icalDataStreams =
                    appointmentCollection
                        .DumpAsIcals(GetATempDirectory())
                        .ReadFiles();
            }
            else
            {
                icalDataStreams =
                    appointmentCollection
                        .SanitizeAppointments(!this.Arguments.IncludeRecurrences)
                        .SerializeIcals();
            }

            if (this.Arguments.SingleFile)
            {
                IEnumerable<Calendar> deserialized = icalDataStreams.DeserializeIcals();

                var calendar = new Calendar();
                foreach (var cal in deserialized)
                {
                    calendar.Events.AddRange(cal.Events);
                }

                icalDataStreams = new[] { calendar }.SerializeIcals();
            }

            string encryptionPassword;
            if (!this.GetEncryptionPassword(out encryptionPassword))
            {
                // specified a password, but something went wrong; abort!
                Console.WriteLine("Failed to get encryption password!");
                return false;
            }

            MemoryStream zfStream = new MemoryStream();
            using (var zf = new ZipFile())
            {
                zf.CompressionLevel = CompressionLevel.BestCompression;
                zf.CompressionMethod = CompressionMethod.BZip2;

                if (!string.IsNullOrEmpty(encryptionPassword))
                {
                    zf.Password = encryptionPassword;
                    zf.Encryption = EncryptionAlgorithm.WinZipAes256;
                }

                int i = 0;
                foreach (var stream in icalDataStreams)
                {
                    string filename = $"{i:D5}.ics";

                    if (this.Arguments.StorageDirectory != null)
                    {
                        string path = Path.Combine(this.Arguments.StorageDirectory, filename);
                        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            stream.CopyTo(fs);
                            stream.Seek(0, SeekOrigin.Begin);
                        }

                        Console.WriteLine($"Dumped to {path}");
                    }
                    zf.AddEntry(filename, stream);

                    i++;
                }

                zf.Save(zfStream);
            }
            zfStream.Seek(0, SeekOrigin.Begin);

            if (this.Arguments.StorageUrl != null)
            {
                Console.WriteLine("Uploading to server...");

                string serverPassphrase;
                if (!GetServerPassphrase(out serverPassphrase))
                {
                    Console.WriteLine("Failed to get server passphrase!");
                    return false;
                }

                using var serverInterface = new ServerInterface(this.Arguments.StorageUrl, this.Arguments.OwnerName,
                    serverPassphrase, this.Arguments.HashName, this.Arguments.CACertificatePath, this.Arguments.CertificatePath);

                if (!serverInterface.AuthenticateOrCreate())
                {
                    Console.WriteLine("Authentication failed!");
                    return false;
                }

                bool result;
                if (this.Arguments.FullSnapshot)
                {
                    result = serverInterface.UploadFullSnapshot(zfStream);
                }
                else
                {
                    result = serverInterface.UploadPartialSnapshot(dtStart.ToUniversalTime(), dtEnd.ToUniversalTime(),
                        zfStream);
                }

                if (!result)
                {
                    Console.WriteLine("Failed to upload snapshot!");
                    return false;
                }

                Console.WriteLine("Upload ok!");
            }

            // *everything* went smoothly

            if (this.Arguments.PartialSinceLastRun)
            {
                SetLastRun(fetchedAt);
            }

            return true;
        }

        private static DateTime? TryGetRelativeOrAbsoluteDatetime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            
            try
            {
                DateTime dt;
                if (Utils.TryParseRelativeDateTime(text, out dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(text, out dt))
                {
                    return dt;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        private static void SetLastRun(DateTime dt)
        {
            string path = Path.Combine(Path.GetTempPath(), "calendarexport_lastrun.txt");
            File.WriteAllText(path, dt.ToString("O"));
        }

        private static DateTime? GetLastRun()
        {
            string path = Path.Combine(Path.GetTempPath(), "calendarexport_lastrun.txt");

            try
            {
                return File.ReadAllText(path).ToDateTime();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetATempDirectory()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        private bool GetEncryptionPassword(out string password)
        {
            password = null;

            try
            {
                if (this.Arguments.EncryptionPasswordFile != null)
                {
                    password = File.ReadAllText(this.Arguments.EncryptionPasswordFile);
                    return true;
                }
                else if (this.Arguments.EncryptionPassword != null)
                {
                    password = this.Arguments.EncryptionPassword;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool GetServerPassphrase(out string passphrase)
        {
            passphrase = null;

            try
            {
                if (this.Arguments.ServerPassphraseFile != null)
                {
                    passphrase = File.ReadAllText(this.Arguments.ServerPassphraseFile);
                    return true;
                }
                else if (this.Arguments.ServerPassphrase != null)
                {
                    passphrase = this.Arguments.ServerPassphrase;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}
