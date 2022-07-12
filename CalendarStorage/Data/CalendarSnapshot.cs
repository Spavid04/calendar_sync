using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CalendarSyncCommons;

namespace CalendarStorage.Data
{
    [Index(nameof(Timestamp), IsUnique = true)]
    [Index(nameof(SnapshotType), IsUnique = false)]
    [Index(nameof(OwnerId), IsUnique = false)]
    public class CalendarSnapshot
    {
        public int Id { get; set; }
        [Required] public string Timestamp { get; set; }
        [Required] public CalendarSnapshotType SnapshotType { get; set; }

        public string? EventModifiedAt_IntervalStart { get; set; }
        public string? EventModifiedAt_IntervalEnd { get; set; }

        [Required] public int OwnerId { get; set; }
        public Owner Owner { get; set; }

        public CalendarBlob DataBlob { get; set; }


        [NotMapped]
        public DateTime TimestampDt
        {
            get => this.Timestamp.ToDateTime();
            set => this.Timestamp = value.ToString("O");
        }
        [NotMapped]
        public DateTime? EventModifiedAt_StartDt
        {
            get => this.EventModifiedAt_IntervalStart?.ToDateTime();
            set => this.EventModifiedAt_IntervalStart = value?.ToString("O");
        }

        [NotMapped]
        public DateTime? EventModifiedAt_EndDt
        {
            get => this.EventModifiedAt_IntervalEnd?.ToDateTime();
            set => this.EventModifiedAt_IntervalEnd = value?.ToString("O");
        }
    }

    public enum CalendarSnapshotType
    {
        Unknown,
        Full,
        Partial
    }
}
