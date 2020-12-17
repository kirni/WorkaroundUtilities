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

        protected IDictionary<WorkaroundPublisherService.WorkaroundActionDelegate, WorkaroundArgs> actions;
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
                //match everything until first parameter (indicated by { ) or until the end
                var key = Regex.Match(inst, @"^([^{])+").ToString();

                if (WorkaroundPublisherService.Actions.Contains(key))
                {
                    if (actions == null)
                    {
                        actions = new Dictionary<WorkaroundPublisherService.WorkaroundActionDelegate, WorkaroundArgs>();
                    }

                    actions.Add(WorkaroundPublisherService.Actions[key].First(), new WorkaroundArgs(inst));
                    _log.LogInformation("{workaround} added action {action}", this, key);
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
                        inst.Key(this, inst.Value);
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

        public static void TerminateProcess(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                WorkaroundWorker worker = (WorkaroundWorker)sender;

                foreach (var procName in args.args)
                {

                    var procs = Process.GetProcessesByName(procName).ToList();

                    if (procs == null || procs.Count <= 0)
                    {
                        worker._log.LogWarning("{workaround} process {process} not found", worker, procName);
                        continue;
                    }

                    procs.ForEach(x => x.Kill());
                    worker._log.LogInformation("{workaround} process {process} killed", worker, procName);
                }
            }
            else
            {
                throw new ArgumentException($"TerminateProcess expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }

        public static void StartProcess(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                WorkaroundWorker worker = (WorkaroundWorker)sender;

                foreach (var procName in args.args)
                {
                    var process = new Process();
                    process.StartInfo.FileName = procName;
                    process.StartInfo.UseShellExecute = true;

                    process.Start();

                    if (process == null)
                    {
                        worker._log.LogInformation("{workaround} failed to start {process}", worker, procName);
                    }
                    else
                    {
                        worker._log.LogInformation("{workaround} process started {process} ID {ID}", worker, procName, process.Id);
                    }

                }
            }
            else
            {
                throw new ArgumentException($"StartProcess expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }

        public static void SendF5(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                WorkaroundWorker worker = (WorkaroundWorker)sender;
                foreach (var procName in args.args)
                {
                    var procs = Process.GetProcessesByName(procName).ToList();

                    if (procs == null || procs.Count <= 0)
                    {
                        worker._log.LogWarning("{workaround} process {process} not found", worker, procName);
                        return;
                    }

                    foreach (Process inst in procs)
                    {
                        if (inst.MainWindowHandle != IntPtr.Zero)
                        {
                            // Set focus on the window so that the key input can be received.
                            SendF5Helper.SetForegroundWindow(inst.MainWindowHandle);

                            // Create a F5 key press
                            SendF5Helper.INPUT ipPress = new SendF5Helper.INPUT { Type = 1 };
                            ipPress.Data.Keyboard = new SendF5Helper.KEYBDINPUT
                            {
                                Vk = (ushort)0x74,  // F5 Key
                                Scan = 0,
                                Flags = 0,
                                //50 ms
                                Time = 0,
                                ExtraInfo = IntPtr.Zero
                            };

                            // Create a F5 key release
                            SendF5Helper.INPUT ipRelease = new SendF5Helper.INPUT { Type = 1 };
                            ipRelease.Data.Keyboard = new SendF5Helper.KEYBDINPUT
                            {
                                Vk = (ushort)0x74,  // F5 Key
                                Scan = 0,
                                Flags = SendF5Helper.KEYEVENTF_KEYUP,
                                //50 ms
                                Time = 0,
                                ExtraInfo = IntPtr.Zero
                            };

                            var inputs = new SendF5Helper.INPUT[] { ipPress, ipRelease };

                            // Send the keypresses to the window
                            SendF5Helper.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(SendF5Helper.INPUT)));

                            worker._log.LogInformation("{workaround} send F5 to process {process}", worker, procName);
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Send keys expects sender of type {typeof(IWorkaroundWorker)}");
            }
        }
    }
}
