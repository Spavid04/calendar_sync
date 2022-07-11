using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CalendarStorage.Data
{
    [Index(nameof(SnapshotId), IsUnique = true)]
    public class CalendarBlob
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }

        [Required] public int SnapshotId { get; set; }
        public CalendarSnapshot Snapshot { get; set; }
    }
}
