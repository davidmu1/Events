using Events.ApplicationService.Validators;
using Events.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.ApplicationService
{
    public class EventsAppService : IEventsAppService
    {
        private string _currentUserName = string.Empty;

        private IEventsRepository _repository;

        public EventsAppService()
        {
        }

        public EventsAppService(string userName)
        {
            this._currentUserName = userName;
            this._repository = new EventsRepository(userName);
        }

        public EventsAppService(IEventsRepository repository, string userName)
        {
        }

        public Event GetEvent(int eventId)
        {
            return this._repository.GetEvent(eventId);
        }

        public IEnumerable<Event> UpcomingEvents(int count)
        {
            return this._repository.UpcomingEvents(count);
        }

        public IEnumerable<Event> GetUserEvents(string activeDirectoryId)
        {
            return this._repository.GetUserEvents(activeDirectoryId);
        }

        public Event CreateEvent(Event eventData)
        {
            if (EventsValidator.ValidateEventData(eventData))
            {
                return this._repository.CreateEvent(eventData);
            }
            else
            {
                throw new ApplicationException("Invalid Data");
            }
        }

        public bool RegisterUser(string activeDirectoryId, int eventId)
        {
            return this._repository.RegisterUser(activeDirectoryId, eventId);
        }
    }
}