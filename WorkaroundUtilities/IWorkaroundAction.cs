using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WorkaroundUtilities
{
    public interface IWorkaroundAction
    {
        public void Execute();
        public void Init(ILogger log, string[] args);

        public bool TryInit(ILogger log, string[] args);

    }

    public class TerminateProcessAction : IWorkaroundAction
    {
        private  ILogger _log;
        private string[] _processNames;

        public void Execute()
        {
            foreach (var procName in _processNames)
            {

                var procs = Process.GetProcessesByName(procName).ToList();

                if (procs == null || procs.Count <= 0)
                {
                    _log.LogWarning("process {process} not found", procName);
                    continue;
                }

                procs.ForEach(x => x.Kill());
                _log.LogInformation("process {process} killed", procName);
            }
        }

        public void Init(ILogger log, string[] args)
        {
            _log = log;
            _processNames = args;            
        }

        public bool TryInit(ILogger log, string[] args)
        {
            Init(log, args);
            //all parameters are valid
            return true;
        }      
    }

    public class StartProcessAction : IWorkaroundAction
    {
        private ILogger _log;
        private string[] _processNames;

        public void Execute()
        {
            foreach (var procName in _processNames)
            {
                var process = new Process();
                process.StartInfo.FileName = procName;
                process.StartInfo.UseShellExecute = true;

                process.Start();

                if (process == null)
                {
                    _log.LogInformation("failed to start {process}", procName);
                }
                else
                {
                    _log.LogInformation("process started {process} ID {ID}", procName, process.Id);
                }

            }
        }

        public void Init(ILogger log, string[] args)
        {
            _log = log;
            _processNames = args;
        }

        public bool TryInit(ILogger log, string[] args)
        {
            var invalid = args.Where(x => File.Exists(x) == false);

            if (invalid != null && invalid.Count() > 0)
            {
                log.LogWarning("executables not found {invalid}", invalid);
                return false;
            }

            Init(log, args);

            return true;
        }
    }
}