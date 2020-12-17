using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WorkaroundUtilities
{
    public interface IWorkaroundEvent
    {
        public bool EventOccurred();

        public void Init(ILogger log, string[] args);

        public bool TryInit(ILogger log, string[] args);
    }

    public class USBconnectedEvent : IWorkaroundEvent
    {
        private string[] _drives;
        private ILogger _log;

        public bool EventOccurred()
        {
            //get all USB sticks
            var found = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);            

            //event valid when any of the connected drives contains any of the args (G:, F:, etc)
            var result = _drives.Where(arg => found.Any(x => x.Name.Contains(arg)));

            if (result != null && result.Count() > 0)
            {
                _log.LogDebug("drives found; found:{foundDrives} configured:{drives}", found, _drives);
                return true;
            }

            _log.LogDebug("no drives found; found:{foundDrives} configured:{drives}", found, _drives);
            return false;
        }

        public void Init(ILogger log, string[] args)
        {
            _log = log;
            _drives = args;
        }

        public bool TryInit(ILogger log, string[] args)
        {
            Init(log, args);
            return true;
        }      
    }

    public class FileExistingEvent : IWorkaroundEvent
    {
        private ILogger _log;
        private string[] _files;
       

        public bool EventOccurred()
        {
            var result = _files.Where(x => File.Exists(x));

            if (result != null && result.Count() > 0)
            {
                _log.LogDebug("files found; found:{foundDrives} configured:{drives}", result, _files);
                return true;
            }

            _log.LogDebug("no files found; found:{foundDrives} configured:{drives}", result, _files);
            return false;
        }

        public void Init(ILogger log, string[] args)
        {
            _log = log;
            _files = args;
        }

        public bool TryInit(ILogger log, string[] args)
        {
            Init(log, args);
            return true;
        }
    }

    public class RAMlimitEvent : IWorkaroundEvent
    {
        private string _procName;
        private long _limit;
        private ILogger _log;

        public bool EventOccurred()
        {       
            var procs = Process.GetProcessesByName(_procName).ToList();

            if (procs == null || procs.Count <= 0)
            {
                _log.LogInformation("process {process} not found", _procName);
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
            if ((sumPagedMemorySize64 / (1024 * 1024)) > _limit)
            {
                return true;
            }

            return false;
        }

        public void Init(ILogger log, string[] args)
        {
            int count = args.Count();
            if (count != 2)
            {
                throw new ArgumentException($"RAMlimitEvent requires two arguments, {count} found");
            }

            _log = log;
            _procName = args[0];
            _limit = long.Parse(args[1]);
        }

        public bool TryInit(ILogger log, string[] args)
        {
            int count = args.Count();
            if (count != 2)
            {
                log.LogError("RAMlimitEvent requires two arguments, {argsCount} found", count);
                return false;
            }

            if (long.TryParse(args[1], out _) == false)
            {
                log.LogError("RAMlimitEvent requires long as second argument, {argsLimit} found", args[1]);
            }

            Init(log, args);
            return true;
        }
    }    
}