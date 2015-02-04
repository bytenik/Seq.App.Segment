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

namespace Seq.Slack
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

        [SeqAppSetting(DisplayName = "User ID property",
            HelpText = "The property used in log events to identify a user. If missing from a log entry, no user will be identified.",
            IsOptional = true)]
        public string UserIdProperty { get; set; }

        public void On(Event<LogEventData> evt)
        {
            string userId = null;
            if (!string.IsNullOrWhiteSpace(UserIdProperty))
            {
                object prop;
                if (evt.Data.Properties.TryGetValue(UserIdProperty, out prop))
                    userId = prop.ToString();
            }

            var properties = new Properties()
            {
                ["Level"] = evt.Data.Level,
                ["EventId"] = evt.Id,
                ["Message"] = evt.Data.RenderedMessage,
            };

            if (evt.Data.Exception != null)
                properties["Exception"] = evt.Data.Exception;

            foreach (var prop in evt.Data.Properties)
                properties[prop.Key] = prop.Value;

            Analytics.Client.Track(userId, evt.Data.MessageTemplate, properties);
        }
    }
}
