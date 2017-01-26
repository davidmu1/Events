namespace Events
{
    using Events.Models;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Web;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

    public class EventsRepository : IEventsRepository
    {
        private const string EventsQuery = "SELECT {0} Id, Title, [Description], Location, StartDate, [Days], AudienceId, OwnerId FROM [Event]";

        private const string RegistrationQuery = @"SELECT Id, Title, [Description], Location, StartDate, [Days], AudienceId, OwnerId
                                                   FROM [Event]
                                                   INNER JOIN [Registration] ON [Event].Id = [Registration].EventId
                                                   WHERE [Registration].UserId = @UserId";

        private const string EventsInsert = @"INSERT INTO Event (Title, Description, Location, StartDate, Days, AudienceId, OwnerId) VALUES (@Title, @Description, @Location, @StartDate, @Days, @AudienceId, @OwnerId);
                                              SELECT @@IDENTITY";

        private const string RegistrationInsert = @"INSERT INTO Registration (UserId, EventId, RegistrationDate) VALUES (@UserId, @EventId, @RegistrationDate)";


        static EventsRepository()
        {

        }

        public EventsRepository(string userName)
        {
        }

        public Event GetEvent(int eventId)
        {
            bool ReadOnly = true;
            using (var cmd = this.CreateCommand(
                string.Format(EventsQuery, string.Empty) + "WHERE Id = @EventId",
                new Dictionary<string, object>() { { "@EventId", eventId } }, ReadOnly))
            {
                return this.EventsFromDBQuery(cmd.ExecuteReader()).FirstOrDefault();
            }
        }

        public IEnumerable<Event> UpcomingEvents(int count)
        {
            bool ReadOnly = true;
            using (var cmd = this.CreateCommand(
                string.Format(EventsQuery, count > 0 ? "TOP " + count : string.Empty) + "WHERE StartDate > GETDATE() ORDER BY StartDate",
                null, ReadOnly))
            {
                return this.EventsFromDBQuery(cmd.ExecuteReader());
            }
        }

        public IEnumerable<Event> GetUserEvents(string activeDirectoryId)
        {
            bool ReadOnly = true;
            using (var cmd = this.CreateCommand(
                RegistrationQuery,
                new Dictionary<string, object>() { { "@UserId", activeDirectoryId } }, ReadOnly))
            {
                var reader = cmd.ExecuteReader();
                return this.EventsFromDBQuery(reader);
            }
        }


        public Event CreateEvent(Event @event)
        {
            using (var cmd = this.CreateCommand(
                EventsInsert,
                new Dictionary<string, object>() {
                { "@Title", @event.Title },
                { "@Description", @event.Description },
                { "@Location", @event.Location },
                { "@StartDate", @event.StartDate },
                { "@Days", @event.Days },
                { "@AudienceId", (int)@event.Audience },
                { "@OwnerId", @event.OwnerId }
                }))
            {
                var id = cmd.ExecuteScalar();
                @event.Id = Convert.ToInt32(id);
                return @event;
            }
        }

        public bool RegisterUser(string activeDirectoryId, int eventId)
        {
            using (var cmd = this.CreateCommand(
                RegistrationInsert,
                new Dictionary<string, object>() {
                { "@UserId", activeDirectoryId },
                { "@EventId", eventId },
                { "@RegistrationDate", DateTime.Now }
                }))
            {
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        private ReliableSqlConnection GetConnection()
        {
            String conString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            ReliableSqlConnection sqlConnection = new ReliableSqlConnection(conString);
            return sqlConnection;
        }

        private IDbCommand CreateCommand(string sqlScript, IDictionary<string, object> @params)
        {
            ReliableSqlConnection connection = GetConnection();
            var command = SqlCommandFactory.CreateCommand(connection);

            command.CommandText = sqlScript;
            command.CommandType = CommandType.Text;

            if (@params != null)
            {
                foreach (var param in @params)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            command.Connection.Open();

            return command;
        }

        private IEnumerable<Event> EventsFromDBQuery(IDataReader reader)
        {
            var events = new List<Event>();

            while (reader.Read())
            {
                events.Add(new Event()
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Location = reader.GetString(3),
                    StartDate = reader.GetDateTime(4),
                    Days = reader.GetInt32(5),
                    Audience = (AudienceType)reader.GetByte(6),
                    OwnerId = reader.GetString(7)
                });
            }

            return events;
        }

        private ReliableSqlConnection GetConnection(bool ReadOny)
        {
            String primary = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            String roSecondary = ConfigurationManager.ConnectionStrings["ROSecondaryConnection"].ConnectionString;
            String conString = String.Empty;
            if (ReadOny == true)
            {
                conString = roSecondary;
                Random r = new Random();
                int random = r.Next();
                if (random % 2 == 0)
                {
                    conString = primary;
                }
                else
                {
                    conString = roSecondary;
                }
            }
            else
            {
                conString = primary;
            }
            ReliableSqlConnection sqlConnection = new ReliableSqlConnection(conString);

            return sqlConnection;
        }

        private IDbCommand CreateCommand(string sqlScript, IDictionary<string, object> @params, bool ReadOnly = false)
        {
            ReliableSqlConnection connection = GetConnection(ReadOnly);
            var command = SqlCommandFactory.CreateCommand(connection);
            command.CommandText = sqlScript;
            command.CommandType = CommandType.Text;
            if (@params != null)
            {
                foreach (var param in @params)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value;
                    command.Parameters.Add(dbParam);
                }
            }
            command.Connection.Open();

            return command;
        }
    }
}