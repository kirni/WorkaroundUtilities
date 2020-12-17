using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace WorkaroundUtilities
{
    public class WorkaroundArgs
    {
        public string[] args { get; private set; } // readonly

        public static string[] ExtractArgs(string text)
        {
            return Regex.Matches(text, @"(?<=\{)(.*?)(?=\})")
                 .Cast<Match>()
                 .Select(m => m.Value)
                 .ToArray();
        }

        public WorkaroundArgs(string text)
        {
            //everything between {} is an argument
            args = Regex.Matches(text, @"(?<=\{)(.*?)(?=\})")
                 .Cast<Match>()
                 .Select(m => m.Value)
                 .ToArray();
        }
    }

    public class WorkaroundServiceOptions
    {
        public WorkaroundDefinition[] workarounds { get; set; }
    }

    public class WorkaroundDefinition
    {
        public float eventpollingSec { get; set; }
        public string description { get; set; }
        public string[] events { get; set; }
        public string[] actions { get; set; }
    }

    public class WorkaroundPublisherService : IWorkaroundPublisherService
    {
        private readonly ILogger<WorkaroundPublisherService> _log;
        private readonly IConfiguration _config;

        public delegate bool WorkaroundEventDelegate(object sender, WorkaroundArgs args);
        public static ILookup<string, WorkaroundEventDelegate> Events = new Dictionary<string, WorkaroundEventDelegate>()
        {
            { "USBconnectedEvent", WorkaroundWorker.USBconnected },
            {"FileExistingEvent", WorkaroundWorker.FileExisting },
            {"RAMlimitEvent", WorkaroundWorker.RAMlimit}

        }.ToLookup(o => o.Key, o => o.Value);     

        public void Run()
        {
            var workarounds = _config.GetSection("workarounds").Get<WorkaroundDefinition[]>();

            foreach (var inst in workarounds)
            {
                IWorkaroundWorker worker = WorkaroundWorker.CreateWorker(_log, inst);

                if (worker.hasActions && worker.hasEvents)
                {
                    var thread = new Thread(worker.Run);
                    thread.Name = worker.ToString();

                    _log.LogDebug("{workaround} create thread {thread}", worker, thread.ManagedThreadId);

                    thread.Start();
                }
            }
        }

        public WorkaroundPublisherService(ILogger<WorkaroundPublisherService> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }
    }
}
