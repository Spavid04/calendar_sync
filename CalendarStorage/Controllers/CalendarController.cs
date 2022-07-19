using System;
using System.Collections.Generic;
using System.Linq;
using CalendarStorage.Data;
using CalendarSyncCommons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalendarStorage.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CalendarController : Controller
    {
        private readonly ILogger<CalendarController> Logger;
        private readonly CalendarStoreContext Storage;

        public CalendarController(ILogger<CalendarController> logger, CalendarStoreContext storage)
        {
            this.Logger = logger;
            this.Storage = storage;
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

        [HttpGet]
        public IActionResult Authenticate(string ownerName, string passphraseHash)
        {
            if (this.GetCheckOwner(ownerName, passphraseHash) != null)
            {
                return NoContent();
            }
            else
            {
                return Unauthorized();
            }
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
                dtStart = modifiedInterval_Start.ToDateTime();
                dtEnd = modifiedInterval_End.ToDateTime();
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
        public ActionResult<IEnumerable<ServerModels.AvailableSnapshot>> GetAvaliableSnapshots(string ownerName, string passphraseHash)
        {
            Owner owner;
            if ((owner = this.GetCheckOwner(ownerName, passphraseHash)) == null)
            {
                return Unauthorized();
            }

            this.UpdateOwnerLastSeen(owner);

            var snaphsots = owner.Snapshots.Select(x => new ServerModels.AvailableSnapshot()
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
}
