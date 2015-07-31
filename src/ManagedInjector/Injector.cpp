// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#include "stdafx.h"

#include "Injector.h"
#include <vcclr.h>
#include <stdio.h>

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

WCHAR DllPath[MAX_PATH] = { 0 };

#pragma data_seg(".shared")
LPTHREAD_START_ROUTINE _routine = 0;
#pragma data_seg()

using namespace System;
using namespace System::IO;
using namespace System::Diagnostics;
using namespace ManagedInjector;

#pragma managed(push, off)

void LoadImagePath()
{
	::GetModuleFileNameW((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));
}

#pragma managed(pop)

String^ GetLogPath()
{
	auto applicationDataPath = Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData);

	if (!Directory::Exists(applicationDataPath))
	{
		Directory::CreateDirectory(applicationDataPath);
	}

	auto logPath = applicationDataPath + "\\ManagedInjector.log";

	return logPath;
}

void Injector::Initialize()
{
	try
	{
		File::Delete(GetLogPath());
	}
	catch (const FileNotFoundException^)
	{
		// ignored
	}
}

void LogMessage(String^ message)
{
	File::AppendAllText(GetLogPath(), DateTime::Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message + Environment::NewLine);
}

void Injector::LogMessage(String^ message)
{
	::LogMessage(message);
}

DWORD StartThread(HANDLE hProcess, LPTHREAD_START_ROUTINE function, wchar_t * data)
{
	auto buffLen = (wcslen(data) + 1) * sizeof(wchar_t);
	void* remoteData = ::VirtualAllocEx(hProcess, NULL, buffLen, MEM_COMMIT, PAGE_READWRITE);

	if (remoteData)
	{
		LogMessage("VirtualAllocEx successful");

		::WriteProcessMemory(hProcess, remoteData, data, buffLen, NULL);

		auto hThread = ::CreateRemoteThread(hProcess, NULL, 0,
			function, remoteData, 0, NULL);

		::WaitForSingleObject(hThread, INFINITE);

		LogMessage("Thread finished");

		DWORD exitCode;
		::GetExitCodeThread(hThread, &exitCode);

		::CloseHandle(hThread);

		::VirtualFreeEx(hProcess, remoteData, 0, MEM_RELEASE);

		return exitCode;
	}

	return 0;
}

void Injector::Launch(Int32 processId, String^ assembly, String^ className, String^ methodName)
{
	auto assemblyClassAndMethod = assembly + "$" + className + "$" + methodName;
	pin_ptr<const wchar_t> acmLocal = PtrToStringChars(assemblyClassAndMethod);

	HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
	if (!hProcess) return;

	LogMessage("Got process handle");

	LoadImagePath();

	auto kernel = ::GetModuleHandle(L"kernel32");
	(HMODULE)StartThread(hProcess, (LPTHREAD_START_ROUTINE)::GetProcAddress(kernel, "LoadLibraryW"), DllPath);
	LogMessage("Library loaded");

	if (_routine)
	{
		StartThread(hProcess, _routine, (wchar_t*)acmLocal);
	}

	::CloseHandle(hProcess);
}

__declspec(dllexport)
DWORD __stdcall ThreadStart(void* param)
{
	LogMessage("Thread started");

	CoInitialize(NULL);

	String^ acmLocal = gcnew String((wchar_t *)param);

	LogMessage(String::Format("acmLocal = {0}", acmLocal));
	cli::array<String^>^ acmSplit = acmLocal->Split('$');

	LogMessage(String::Format("About to load assembly {0}", acmSplit[0]));
	auto assembly = Reflection::Assembly::LoadFile(acmSplit[0]);
	if (assembly == nullptr) return 0;

	LogMessage(String::Format("About to load type {0}", acmSplit[1]));
	auto type = assembly->GetType(acmSplit[1]);
	if (type == nullptr) return 0;

	LogMessage(String::Format("Just loaded the type {0}", acmSplit[1]));
	auto methodInfo = type->GetMethod(acmSplit[2], Reflection::BindingFlags::Static | Reflection::BindingFlags::Public);
	if (methodInfo == nullptr) return 0;

	LogMessage(String::Format("About to invoke {0} on type {1}", methodInfo->Name, acmSplit[1]));
	auto returnValue = methodInfo->Invoke(nullptr, nullptr);
	if (returnValue == nullptr)
	{
		returnValue = "NULL";
	}
	LogMessage(String::Format("Return value of {0} on type {1} is {2}", methodInfo->Name, acmSplit[1], returnValue));

	return 0;
}

#pragma managed(push, off)

BOOLEAN WINAPI DllMain(IN HINSTANCE hDllHandle,
	IN DWORD     nReason,
	IN LPVOID    Reserved)
{
	_routine = (LPTHREAD_START_ROUTINE)&ThreadStart;
	return true;
}

#pragma managed(pop)
