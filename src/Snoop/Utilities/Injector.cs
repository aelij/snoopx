// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Snoop.Utilities
{
	internal static class Injector
	{
	    internal static void Launch(int processId, Assembly assembly, string className, string methodName)
		{
			var location = Assembly.GetEntryAssembly().Location;
			var directory = Path.GetDirectoryName(location);
		    // ReSharper disable once AssignNullToNotNullAttribute
            var file = Path.Combine(directory, "ManagedInjectorLauncher" + GetSuffix(processId) + ".exe");

			Process.Start(file, $"{processId} \"{assembly.Location}\" \"{className}\" \"{methodName}\"");
		}

	    private static string GetSuffix(int processId)
	    {
	        var bitness = IntPtr.Size == 8 ? "64" : "32";
	        const string clr = "4.0";

	        // sometimes the module names aren't always the same case. compare case insensitive.
	        // see for more info: http://snoopwpf.codeplex.com/workitem/6090
	        if (WindowInfo.GetModulesByProcessId(processId)
	            .Any(module => module.szModule.Contains("wow64.dll") &&
	                           FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3))
	        {
	            bitness = "32";
	        }
	        return $"{bitness}-{clr}";
	    }
	}
}
