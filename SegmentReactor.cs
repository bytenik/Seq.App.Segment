using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Segment;
using Segment.Model;

namespace Seq.Segment
{
    [SeqApp("Segment.io Relayer", Description = "Sends messages matching a view to Segment.io")]
    public class SegmentReactor : Reactor, ISubscribeTo<LogEventData>
    {
        private string _segmentKey;

        [SeqAppSetting(
            DisplayName = "Key",
            HelpText = "Your Segment.io Key")]
        public string SegmentKey
        {
            get { return _segmentKey; }
            set
            {
                _segmentKey = value;
                Analytics.Initialize(_segmentKey);
            }
        }

        [SeqAppSetting(DisplayName = "User ID Property",
            HelpText = "The property used in log events to identify a user. If missing from a log entry, or not specified, the default user id will be used.",
            IsOptional = true)]
        public string UserIdProperty { get; set; }

        [SeqAppSetting(DisplayName = "Default User ID",
            HelpText = "The default user ID used to identify a user.")]
        public string DefaultUserId { get; set; } = "Seq";

        [SeqAppSetting(DisplayName = "Event Type Property",
            HelpText = "The property used in log events to choose the event type for Segment. If this property is not specified or is missing from an event, and default event type is not specified, the message template will be used. This property will have priority over the default event type when not missing from an log event.",
            IsOptional = true)]
        public string EventTypeProperty { get; set; }

        [SeqAppSetting(DisplayName = "Default Event Type",
            HelpText = "The default event type used to identify events in Segment. The event type property, when specified and present in an event, will take priority. The message template will be used if this is not specified, and the event type property is not present on an event or not specified.",
            IsOptional = true)]
        public string DefaultEventType { get; set; }

        public void On(Event<LogEventData> evt)
        {
            string userId;
            if (!string.IsNullOrWhiteSpace(UserIdProperty) && evt.Data.Properties != null && evt.Data.Properties.ContainsKey(UserIdProperty))
                userId = evt.Data.Properties[UserIdProperty].ToString();
            else
                userId = DefaultUserId;

            var properties = new Properties()
            {
                ["Level"] = evt.Data.Level,
                ["EventId"] = evt.Id,
                ["Message"] = evt.Data.RenderedMessage,
            };

            if (evt.Data.Exception != null)
                properties["Exception"] = evt.Data.Exception;

            if (evt.Data.Properties != null) foreach (var prop in evt.Data.Properties)
                properties[prop.Key] = prop.Value;

            string eventType;
            if (!string.IsNullOrWhiteSpace(EventTypeProperty) && evt.Data.Properties != null && evt.Data.Properties.ContainsKey(EventTypeProperty))
                eventType = evt.Data.Properties[EventTypeProperty].ToString();
            else if (!string.IsNullOrWhiteSpace(DefaultEventType))
                eventType = DefaultEventType;
            else
                eventType = evt.Data.MessageTemplate;

            Analytics.Client.Track(userId ?? "Seq", eventType, properties);
        }
    }
}
