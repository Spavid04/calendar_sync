using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalendarStorage.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalendarStorage.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CalendarController : Controller
    {
        private readonly ILogger<CalendarController> Logger;
        private CalendarStoreContext Storage;

        public CalendarController(ILogger<CalendarController> logger)
        {
            this.Logger = logger;
            this.Storage = new CalendarStoreContext();
        }

        [HttpGet]
        public IActionResult Alive()
        {
            return Ok();
        }

        private Owner? GetOwner(string ownerName)
        {
            return this.Storage.Owners
                .Include(x => x.Snapshots)
                .FirstOrDefault(x => x.Name == ownerName);
        }

        private Owner? CreateOwner(string ownerName, string passphraseHash)
        {
            var owner = this.GetOwner(ownerName);
            if (owner != null)
            {
                return null;
            }

            owner = new Owner()
            {
                Name = ownerName,
                PassphraseHash = passphraseHash,
                LastSeenDt = DateTime.UtcNow
            };
            this.Storage.Owners.Add(owner);
            this.Storage.SaveChanges();

            return this.GetOwner(ownerName);
        }

        private Owner? GetCheckOwner(string ownerName, string passphraseHash)
        {
            var owner = this.GetOwner(ownerName);
            if (owner == null)
            {
                return null;
            }

            if (owner.PassphraseHash != passphraseHash)
            {
                return null;
            }

            return owner;
        }

        private bool UpdateOwnerLastSeen(Owner owner)
        {
            if (owner == null)
            {
                return false;
            }

            owner.LastSeenDt = DateTime.UtcNow;
            this.Storage.SaveChanges();
            return true;
        }

        private void AddSnapshotInternal(string ownerName, string passphraseHash, CalendarSnapshotType type,
            DateTime? modifiedInterval_Start, DateTime? modifiedInterval_End, byte[] data)
        {
            var owner = this.GetOwner(ownerName);
            var snapshot = new CalendarSnapshot()
            {
                TimestampDt = DateTime.UtcNow,
                SnapshotType = type,
                EventModifiedAt_StartDt = modifiedInterval_Start?.ToUniversalTime(),
                EventModifiedAt_EndDt = modifiedInterval_End?.ToUniversalTime(),
                Owner = owner,
                DataBlob = new CalendarBlob()
                {
                    Data = data
                }
            };
            this.Storage.Snapshots.Add(snapshot);
            this.Storage.SaveChanges();

            this.UpdateOwnerLastSeen(owner);
        }

        [HttpPost]
        public IActionResult ReserveName([FromQuery] string ownerName, [FromQuery] string passphraseHash)
        {
            Owner owner;
            lock (this.Storage)
            {
                owner = this.CreateOwner(ownerName, passphraseHash);
            }

            if (owner == null)
            {
                if (GetCheckOwner(ownerName, passphraseHash) == null)
                {
                    return Unauthorized();
                }
            }

            return NoContent();
        }

        [HttpPost]
        public IActionResult AddPartialSnapshot([FromQuery] string ownerName, [FromQuery] string passphraseHash,
            [FromQuery] string modifiedInterval_Start, [FromQuery] string modifiedInterval_End, [FromBody] byte[] data)
        {
            Owner owner;
            if ((owner = this.GetCheckOwner(ownerName, passphraseHash)) == null)
            {
                return Unauthorized();
            }

            if (data == null || data.Length == 0)
            {
                return BadRequest();
            }

            DateTime dtStart, dtEnd;
            try
            {
                dtStart = DateTime.Parse(modifiedInterval_Start);
                dtEnd = DateTime.Parse(modifiedInterval_End);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            this.AddSnapshotInternal(ownerName, passphraseHash, CalendarSnapshotType.Partial, dtStart, dtEnd, data);

            return NoContent();
        }

        [HttpPost]
        public IActionResult AddFullSnapshot([FromQuery] string ownerName, [FromQuery] string passphraseHash, [FromBody] byte[] data)
        {
            Owner owner;
            if ((owner = this.GetCheckOwner(ownerName, passphraseHash)) == null)
            {
                return Unauthorized();
            }

            if (data == null || data.Length == 0)
            {
                return BadRequest();
            }

            this.AddSnapshotInternal(ownerName, passphraseHash, CalendarSnapshotType.Full, null, null, data);

            return NoContent();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ControllerModels.AvailableSnapshot>> GetAvaliableSnapshots(string ownerName, string passphraseHash)
        {
            Owner owner;
            if ((owner = this.GetCheckOwner(ownerName, passphraseHash)) == null)
            {
                return Unauthorized();
            }

            this.UpdateOwnerLastSeen(owner);

            var snaphsots = owner.Snapshots.Select(x => new ControllerModels.AvailableSnapshot()
            {
                Id = x.Id,
                Timestamp = x.TimestampDt.ToString("O"),
                SnapshotType = x.SnapshotType.ToString(),
                EventModifiedAt_IntervalStart = x.EventModifiedAt_StartDt?.ToString("O"),
                EventModifiedAt_IntervalEnd = x.EventModifiedAt_EndDt?.ToString("O"),
            });

            return Ok(snaphsots);
        }

        [HttpGet]
        public ActionResult<byte[]> GetSnapshotData(string ownerName, string passphraseHash, int id)
        {
            Owner owner;
            if ((owner = this.GetCheckOwner(ownerName, passphraseHash)) == null)
            {
                return Unauthorized();
            }

            this.UpdateOwnerLastSeen(owner);

            var snapshot = this.Storage.Snapshots.Include(x => x.DataBlob)
                .FirstOrDefault(x => x.Id == id);
            if (snapshot == null)
            {
                return NotFound();
            }

            return Ok(snapshot.DataBlob.Data);
        }
    }

    public static class ControllerModels
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
