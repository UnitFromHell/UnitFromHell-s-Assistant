using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitFromHellBot.Models
{
    public class ReminderEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long ChatId { get; set; }
        public DateTime TargetTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsNotified { get; set; } = false;

    }
}
