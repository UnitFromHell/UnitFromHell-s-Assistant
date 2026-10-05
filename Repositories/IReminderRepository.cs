using System;
using System.Collections.Generic;
using UnitFromHellBot.Models;

namespace UnitFromHellBot.Repositories
{
    public interface IReminderRepository
    {
        void AddEvent(ReminderEvent reminderEvent);
        List<ReminderEvent> GetAllEvents();
        ReminderEvent? GetEventById(Guid id);
        void UpdateEvent(ReminderEvent reminderEvent);
        void RemoveEvent(Guid id);
    }
}
