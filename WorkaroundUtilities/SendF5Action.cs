using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Linq;

namespace WorkaroundUtilities
{
    public class SendF5Action : IWorkaroundAction
    {
        private ILogger _log;
        private string[] _processNames;
        public void Execute()
        {
            foreach (var procName in _processNames)
            {
                var procs = Process.GetProcessesByName(procName).ToList();

                if (procs == null || procs.Count <= 0)
                {
                    _log.LogWarning("{process {process} not found", procName);
                    return;
                }

                foreach (Process inst in procs)
                {
                    if (inst.MainWindowHandle != IntPtr.Zero)
                    {
                        // Set focus on the window so that the key input can be received.
                        SetForegroundWindow(inst.MainWindowHandle);

                        // Create a F5 key press
                        INPUT ipPress = new INPUT { Type = 1 };
                        ipPress.Data.Keyboard = new KEYBDINPUT
                        {
                            Vk = (ushort)0x74,  // F5 Key
                            Scan = 0,
                            Flags = 0,
                            //50 ms
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        };

                        // Create a F5 key release
                        INPUT ipRelease = new INPUT { Type = 1 };
                        ipRelease.Data.Keyboard = new KEYBDINPUT
                        {
                            Vk = (ushort)0x74,  // F5 Key
                            Scan = 0,
                            Flags = KEYEVENTF_KEYUP,
                            //50 ms
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        };

                        var inputs = new INPUT[] { ipPress, ipRelease };

                        // Send the keypresses to the window
                        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                        _log.LogInformation("send F5 to process {process}", procName);
                    }
                }
            }
        }

        public static void SendF5(object sender, WorkaroundArgs args)
        {
            if (sender is IWorkaroundWorker)
            {
                
            }
            else
            {
                throw new ArgumentException($"Send keys expects sender of type {typeof(IWorkaroundWorker)}");
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

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        internal const uint KEYEVENTF_SCANCODE = 0x0008;
        internal const uint KEYEVENTF_UNICODE = 0x0004;

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646270(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/Forums/en/csharplanguage/thread/f0e82d6e-4999-4d22-b3d3-32b25f61fb2a
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/netfxbcl/thread/2abc6be8-c593-4686-93d2-89785232dacd
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
    }
}