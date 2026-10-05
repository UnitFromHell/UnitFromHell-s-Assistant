using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UnitFromHellBot.Models;

namespace UnitFromHellBot.Repositories
{
    public class SqliteReminderRepository : IReminderRepository
    {
        private readonly string _connectionString = "Data Source=reminders.db";

        public SqliteReminderRepository()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Reminders (
                    Id TEXT PRIMARY KEY,
                    ChatId INTEGER NOT NULL,
                    TargetTime TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    IsNotified INTEGER NOT NULL
                );";

            using var command = new SqliteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        public void AddEvent(ReminderEvent reminderEvent)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "INSERT INTO Reminders (Id, ChatId, TargetTime, Description, IsNotified) VALUES (@Id, @ChatId, @TargetTime, @Description, @IsNotified);";
            using var command = new SqliteCommand(query, connection);

            command.Parameters.AddWithValue("@Id", reminderEvent.Id.ToString());
            command.Parameters.AddWithValue("@ChatId", reminderEvent.ChatId);
            command.Parameters.AddWithValue("@TargetTime", reminderEvent.TargetTime.ToString("o")); 
            command.Parameters.AddWithValue("@Description", reminderEvent.Description);
            command.Parameters.AddWithValue("@IsNotified", reminderEvent.IsNotified ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public List<ReminderEvent> GetAllEvents()
        {
            var list = new List<ReminderEvent>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT Id, ChatId, TargetTime, Description, IsNotified FROM Reminders;";
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ReminderEvent
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ChatId = reader.GetInt64(1),
                    TargetTime = DateTime.Parse(reader.GetString(2)),
                    Description = reader.GetString(3),
                    IsNotified = reader.GetInt32(4) == 1
                });
            }

            return list;
        }

        public ReminderEvent? GetEventById(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "SELECT Id, ChatId, TargetTime, Description, IsNotified FROM Reminders WHERE Id = @Id;";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ReminderEvent
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ChatId = reader.GetInt64(1),
                    TargetTime = DateTime.Parse(reader.GetString(2)),
                    Description = reader.GetString(3),
                    IsNotified = reader.GetInt32(4) == 1
                };
            }

            return null;
        }

        public void UpdateEvent(ReminderEvent reminderEvent)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"
                UPDATE Reminders 
                SET TargetTime = @TargetTime, 
                    Description = @Description, 
                    IsNotified = @IsNotified 
                WHERE Id = @Id;";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", reminderEvent.Id.ToString());
            command.Parameters.AddWithValue("@TargetTime", reminderEvent.TargetTime.ToString("o"));
            command.Parameters.AddWithValue("@Description", reminderEvent.Description);
            command.Parameters.AddWithValue("@IsNotified", reminderEvent.IsNotified ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public void RemoveEvent(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = "DELETE FROM Reminders WHERE Id = @Id;";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
        }
    }
}
