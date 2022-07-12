using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CalendarSyncCommons;

namespace CalendarStorage.Data
{
    [Index(nameof(Name), IsUnique = true)]
    public class Owner
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string PassphraseHash { get; set; }
        [Required] public string LastSeen { get; set; }


        public List<CalendarSnapshot> Snapshots { get; set; }


        [NotMapped]
        public DateTime LastSeenDt
        {
            get => this.LastSeen.ToDateTime();
            set => this.LastSeen = value.ToString("O");
        }
    }
}
