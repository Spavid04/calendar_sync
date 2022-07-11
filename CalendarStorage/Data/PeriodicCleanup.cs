using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarStorage.Data
{
    public class PeriodicCleanup : IHostedService, IDisposable
    {
        private Timer TheTimer = null;
        private readonly CalendarStoreContext Storage;

        public PeriodicCleanup()
        {
            this.Storage = new CalendarStoreContext();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            long cleanupInterval = EnvConfig.CleanupInterval ?? 3600;

            if (cleanupInterval >= 0)
            {
                cleanupInterval = Math.Clamp(cleanupInterval, 60, cleanupInterval); // once every minute is the minimum...
                this.TheTimer = new Timer(this.Cleanup, null, cleanupInterval * 1000, cleanupInterval * 1000);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.TheTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            this.TheTimer?.Dispose();
            this.Storage.Dispose();
        }

        private void Cleanup(object state)
        {
            int maxPartialSs = EnvConfig.MaxPartialSnapshots ?? 10;
            int maxFullSs = EnvConfig.MaxPartialSnapshots ?? 1;
            int maxAge = EnvConfig.MaxSnapshotAge ?? 86400;
            var now = DateTime.UtcNow;

            var toRemove = new List<CalendarSnapshot>();

            foreach(var owner in this.Storage.Owners)
            {
                int partialSs = 0;
                int fullSs = 0;

                foreach(var snapshot in owner.Snapshots)
                {
                    if (GetAge(now, snapshot.TimestampDt) > maxAge)
                    {
                        toRemove.Append(snapshot);
                    }
                    else
                    {
                        switch (snapshot.SnapshotType)
                        {
                            case CalendarSnapshotType.Unknown:
                                toRemove.Append(snapshot);
                                break;
                            case CalendarSnapshotType.Full:
                                fullSs++;
                                break;
                            case CalendarSnapshotType.Partial:
                                partialSs++;
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                if (partialSs > maxPartialSs)
                {
                    toRemove.AddRange(owner.Snapshots.OrderBy(x => x.Timestamp).Take(partialSs - maxPartialSs));
                }

                if (fullSs > maxFullSs)
                {
                    toRemove.AddRange(owner.Snapshots.OrderBy(x => x.Timestamp).Take(fullSs - maxFullSs));
                }
            }

            if (toRemove.Count > 0)
            {
                this.Storage.Snapshots.RemoveRange(toRemove);
                this.Storage.SaveChanges();
            }

            if (EnvConfig.DeleteEmptyOwners ?? true)
            {
                DateTime cutoffDt = DateTime.UtcNow - TimeSpan.FromDays(30);
                bool hasRemoved = false;
                foreach (var owner in this.Storage.Owners)
                {
                    if ((!owner.Snapshots.Any()) && owner.LastSeenDt <= cutoffDt)
                    {
                        this.Storage.Owners.Remove(owner);
                        hasRemoved = true;
                    }
                }

                if (hasRemoved)
                {
                    this.Storage.SaveChanges();
                }
            }
        }

        private static int GetAge(DateTime now, DateTime what)
        {
            return (int)(now - what).TotalSeconds;
        }
    }
}
