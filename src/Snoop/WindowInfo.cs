using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Snoop
{
    public class WindowInfo
    {
        private static readonly Dictionary<int, bool> _processIdToValidityMap = new Dictionary<int, bool>();

        public WindowInfo(IntPtr hwnd)
        {
            _hwnd = hwnd;			
        }

        public static void ClearCachedProcessInfo()
        {
            _processIdToValidityMap.Clear();
        }

        public event EventHandler<AttachFailedEventArgs> AttachFailed;

        public IEnumerable<NativeMethods.MODULEENTRY32> Modules
        {
            get
            {
                if (_modules == null)
                {
                    _modules = GetModules().ToArray();
                }
                return _modules;
            }
        }

        private IEnumerable<NativeMethods.MODULEENTRY32> _modules;

        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
        {
            int processId;
            NativeMethods.GetWindowThreadProcessId(_hwnd, out processId);

            var me32 = new NativeMethods.MODULEENTRY32();
            var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);
            if (!hModuleSnap.IsInvalid)
            {
                using (hModuleSnap)
                {
                    me32.dwSize = (uint)Marshal.SizeOf(me32);
                    if (NativeMethods.Module32First(hModuleSnap, ref me32))
                    {
                        do
                        {
                            yield return me32;
                        } while (NativeMethods.Module32Next(hModuleSnap, ref me32));
                    }
                }
            }
        }

        public bool IsValidProcess
        {
            get
            {
                bool isValid = false;
                try
                {
                    if (_hwnd == IntPtr.Zero)
                        return false;

                    Process process = OwningProcess;
                    if (process == null)
                        return false;

                    // see if we have cached the process validity previously, if so, return it.
                    if (_processIdToValidityMap.TryGetValue(process.Id, out isValid))
                        return isValid;

                    // else determine the process validity and cache it.
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        isValid = false;

                        // the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
                        // to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
                        // well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
                        // in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
                        // since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
                        // second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
                    }
                    else
                    {
                        // a process is valid to snoop if it contains a dependency on clr.dll
                        if (Modules.Any(module => module.szModule.StartsWith("clr", StringComparison.OrdinalIgnoreCase)))
                        {
                            isValid = true;
                        }
                    }

                    _processIdToValidityMap[process.Id] = isValid;
                }
                catch
                {
                    // ignored
                }
                return isValid;
            }
        }

        public Process OwningProcess
        {
            get { return NativeMethods.GetWindowThreadProcess(_hwnd); }
        }

        private readonly IntPtr _hwnd;

        public IntPtr HWnd
        {
            get { return _hwnd; }
        }

        public string Description
        {
            get
            {
                Process process = OwningProcess;
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id + "]";
                }
                return process.ProcessName + " [" + process.Id + "]";
            }
        }

        public override string ToString()
        {
            return Description;
        }

        public void Snoop()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Injector.Launch(HWnd, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, "GoBabyGo");
            }
            catch (Exception e)
            {
                OnFailedToAttach(e);
            }
            Mouse.OverrideCursor = null;
        }

        public void Magnify()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Injector.Launch(HWnd, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, "GoBabyGo");
            }
            catch (Exception e)
            {
                OnFailedToAttach(e);
            }
            Mouse.OverrideCursor = null;
        }

        private void OnFailedToAttach(Exception e)
        {
            var handler = AttachFailed;
            if (handler != null) handler(this, new AttachFailedEventArgs(e, Description));
        }
    }
}