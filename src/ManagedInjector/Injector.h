// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma once

using namespace System;

__declspec(dllexport)
DWORD __stdcall ThreadStart(void* param);

namespace ManagedInjector
{
	public ref class Injector sealed : Object
	{
	public:
		static void Initialize();
		static void Launch(Int32 processId, String^ assemblyName, String^ className, String^ methodName);
		static void LogMessage(String^ message);
	};
}