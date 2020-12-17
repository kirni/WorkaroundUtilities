using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;

namespace WorkaroundUtilities
{
    public class WorkaroundWorker : IWorkaroundWorker
    {
        protected readonly ILogger<WorkaroundPublisherService> _log;
        protected readonly WorkaroundDefinition _definition;

        private readonly WorkaroundActionFactory actionFactory = new WorkaroundActionFactory();
        
        protected ICollection<IWorkaroundAction> actions;
        protected IDictionary<WorkaroundPublisherService.WorkaroundEventDelegate, WorkaroundArgs> events;

        public bool hasActions
        {
            get
            {
                return actions == null ? false : actions.Count > 0;
            }
        }
        public bool hasEvents
        {
            get
            {
                return events == null ? false : actions.Count > 0;
            }
        }

        public WorkaroundWorker(ILogger<WorkaroundPublisherService> log, WorkaroundDefinition definition)
        {
            _log = log;
            _definition = definition;

            _log.LogInformation("{workaround} initialize", this);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            _log.LogInformation("{workaround} settings {settings}", this, JsonSerializer.Serialize(_definition, options));

            foreach (var inst in _definition.events)
            {
                //match everything until first parameter (indicated by { ) or until the end
                var key = Regex.Match(inst, @"^([^{])+").ToString();

                if (WorkaroundPublisherService.Events.Contains(key))
                {
                    if (events == null)
                    {
                        events = new Dictionary<WorkaroundPublisherService.WorkaroundEventDelegate, WorkaroundArgs>();
                    }

                    events.Add(WorkaroundPublisherService.Events[key].First(), new WorkaroundArgs(inst));
                    _log.LogInformation("{workaround} added event {event}", this, key);
                }
                else
                {
                    _log.LogWarning("{workaround} skipped unkown event {unknownEvent}", this, key);
                }
            }

            foreach (var inst in _definition.actions)
            {
                //match everything until first parameter (indicated by "{") or until the end
                var key = Regex.Match(inst, @"^([^{])+").ToString();

                if (actionFactory.KnowsType(key))
                {
                    if (actions == null)
                    {
                        actions = new List<IWorkaroundAction>();
                    }

                    IWorkaroundAction temp = actionFactory.Create(key);

                    if (temp.TryInit(_log, WorkaroundArgs.ExtractArgs(inst)) == true)
                    {

                        actions.Add(temp);
                        _log.LogInformation("{workaround} added action {action}", this, temp);
                    }
                    else
                    {
                        _log.LogWarning("{workaround} skip invalid action {actionDefinition}", this, inst);
                    }
                }
                else
                {
                    _log.LogWarning("{workaround} skipped unkown action {unknownAction}", this, key);
                }
            }

            if (events == null)
            {
                _log.LogError("{workaround} definition has no valid events; worker execution skipped", this);
            }
            if (actions == null)
            {
                _log.LogError("{workaround} definition has no valid actions; worker execution skipped", this);
            }
        }

        internal static IWorkaroundWorker CreateWorker(ILogger<WorkaroundPublisherService> log, WorkaroundDefinition inst)
        {
            return new WorkaroundWorker(log, inst);
        }

        public void Run()
        {
            _log.LogInformation("{workaround} start execution", this);

            do
            {
                Thread.Sleep((int)(_definition.eventpollingSec * 1000));

                //call all actions only if all events are true
                if (events.All(x => x.Key(this, x.Value) == true))
                {
                    foreach (var inst in actions)
                    {
                        inst.Execute();
                    }
                }
            } while (true);
        }

        public override string ToString()
        {
            return _definition.description;
        }

        public static bool USBconnected(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                //get all USB sticks
                var drives = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);

                //event valid when any of the connected drives contains any of the args (G:, F:, etc)
                return args.args.Any(arg => drives.Any(x => x.Name.Contains(arg)));
            }
            else
            {
                throw new ArgumentException($"USBconnected expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }

        public static bool FileExisting(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                WorkaroundWorker worker = (WorkaroundWorker)sender;
                return args.args.Any(x => File.Exists(x));
            }
            else
            {
                throw new ArgumentException($"FileExisting expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }

        public static bool RAMlimit(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                WorkaroundWorker worker = (WorkaroundWorker)sender;

                string procName = args.args[0];
                long limit = long.Parse(args.args[1]);

                var procs = Process.GetProcessesByName(procName).ToList();

                if (procs == null || procs.Count <= 0)
                {
                    worker._log.LogWarning("{workaround} process {process} not found", worker, procName);
                    return false;
                }

                long sumWorkingSet64 = 0;
                long sumPagedMemorySize64 = 0;

                foreach (Process proc in procs)
                {
                    sumWorkingSet64 += proc.WorkingSet64;
                    sumPagedMemorySize64 += proc.PagedMemorySize64;
                }

                //calculate in MB
                if ((sumPagedMemorySize64 / (1024 * 1024)) > limit)
                {
                    return true;
                }

                return false;
            }
            else
            {
                throw new ArgumentException($"RAMlimit expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }                    
    }
}
