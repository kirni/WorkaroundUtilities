using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        public WorkaroundDefinition[] Workarounds { get; set; }
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

        private IDictionary<IWorkaroundWorker, Thread> _workers;

        public void Run()
        {
            var workarounds = _config.GetSection("workarounds").Get<WorkaroundDefinition[]>();

            _workers = new Dictionary<IWorkaroundWorker, Thread>();          

            foreach (var inst in workarounds)
            {
                IWorkaroundWorker worker = WorkaroundWorker.CreateWorker(_log, inst);

                if (worker.hasActions && worker.hasEvents)
                {
                    var thread = new Thread(worker.Run)
                    {
                        Name = worker.ToString()
                    };

                    _log.LogDebug("create thread {thread}", thread.Name);

                    _workers.Add(worker, thread);
                    thread.Start();
                }
            }

            //_config.GetReloadToken().RegisterChangeCallback(Restart, null);
        }

        /*private void Restart(object obj)
        {
            _log.LogInformation("reload appsettings");
            foreach (var set in _workers)
            {
                set.Key.Stop();

            }

            while (_workers.Values.Any(x => x.IsAlive)) ;

            _workers.Clear();

            Run();
        }*/

        public WorkaroundPublisherService(ILogger<WorkaroundPublisherService> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }
    }
}
