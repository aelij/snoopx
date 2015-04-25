// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using ManagedInjector;

namespace ManagedInjectorLauncher
{
	internal static class Program
	{
		static void Main(string[] args)
		{
		    Injector.Initialize();

            Injector.LogMessage("Starting the injection process...");

		    if (args.Length != 4)
		    {
		        Injector.LogMessage("Invalid args");
		        return;
		    }

			var processId = Int32.Parse(args[0]);
			var assemblyName = args[1];
			var className = args[2];
			var methodName = args[3];

			Injector.Launch(processId, assemblyName, className, methodName);
		}
	}
}
