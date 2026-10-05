using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitFromHellBot.Models
{
    public enum SessionState
    {
        Idle,
        ChoosingDate,
        ChoosingHour,
        ChoosingMinute,
        AwaitingDescription
    }

    public class UserSession
    {
        public SessionState State { get; set; } = SessionState.Idle;
        public int TargetYear { get; set; } = DateTime.Now.Year;
        public int TargetMonth { get; set; } = DateTime.Now.Month;
        public int TargetDay { get; set; }
        public int TargetHour { get; set; }
        public int TargetMinute { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid? EditingEventId { get; set; }
    }
}
