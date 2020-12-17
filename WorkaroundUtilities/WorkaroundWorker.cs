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

        private readonly WorkaroundActionFactory _actionFactory = new WorkaroundActionFactory();
        private readonly WorkaroundEventFactory _eventFactory = new WorkaroundEventFactory();
        
        protected ICollection<IWorkaroundAction> actions;
        protected ICollection<IWorkaroundEvent> events;

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

            _log.LogDebug("initialize");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            _log.LogDebug("settings {settings}", JsonSerializer.Serialize(_definition, options));

            foreach (var inst in _definition.events)
            {
                //match everything until first parameter (indicated by { ) or until the end
                var key = Regex.Match(inst, @"^([^{])+").ToString();

                if (_eventFactory.KnowsType(key))
                {
                    if (events == null)
                    {
                        events = new List<IWorkaroundEvent>();
                    }

                    IWorkaroundEvent temp = _eventFactory.Create(key);

                    if (temp.TryInit(_log, WorkaroundArgs.ExtractArgs(inst)) == true)
                    {

                        events.Add(temp);
                        _log.LogInformation("added event {event}", temp);
                    }
                    else
                    {
                        _log.LogWarning("skip invalid action {eventDefinition}", inst);
                    }
                }
                else
                {
                    _log.LogWarning("skipped unkown event {unknownEvent}", key);
                }
            }

            foreach (var inst in _definition.actions)
            {
                //match everything until first parameter (indicated by "{") or until the end
                var key = Regex.Match(inst, @"^([^{])+").ToString();

                if (_actionFactory.KnowsType(key))
                {
                    if (actions == null)
                    {
                        actions = new List<IWorkaroundAction>();
                    }

                    IWorkaroundAction temp = _actionFactory.Create(key);

                    if (temp.TryInit(_log, WorkaroundArgs.ExtractArgs(inst)) == true)
                    {

                        actions.Add(temp);
                        _log.LogInformation("added action {action}", temp);
                    }
                    else
                    {
                        _log.LogWarning("skip invalid action {actionDefinition}", inst);
                    }
                }
                else
                {
                    _log.LogWarning("skipped unkown action {unknownAction}", key);
                }
            }

            if (events == null)
            {
                _log.LogError("definition has no valid events; worker execution skipped");
            }
            if (actions == null)
            {
                _log.LogError("definition has no valid actions; worker execution skipped");
            }
        }

        internal static IWorkaroundWorker CreateWorker(ILogger<WorkaroundPublisherService> log, WorkaroundDefinition inst)
        {
            return new WorkaroundWorker(log, inst);
        }

        public void Run()
        {
            _log.LogInformation("start execution");

            do
            {
                Thread.Sleep((int)(_definition.eventpollingSec * 1000));

                //call all actions only if all events are true
                if (events.All(x => x.EventOccurred()))
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
    }
}
