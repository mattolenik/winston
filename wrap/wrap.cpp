// wrap.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#pragma pack(push, 1)
struct Options {
	// Unicode isn't needed here but is used to keep .NET interop simple.
	const _TCHAR magic[32];

	// Fixed lengths to keep things simple. 128 characters
	// should be more than enough given that the paths are relative.
	const _TCHAR appPath[128];
	const _TCHAR workingDir[128];

	// Must be 4 byte bool for easy .NET interop
	BOOL WaitForCompletion;
};
#pragma pack(pop)

// This stuct gets filled by winston during the linking stage.
// This program serves as a template. Winston looks for the location
// of the magic number and then writes a new struct into the binary
// with the correct data prepopulated.
static const struct Options Opts = {
	_T("24cf2af931624d70b7972221e1fa1df"),
	_T("                                                                                                                               "),
	_T("                                                                                                                               "),
	true };

int _tmain(int argc, _TCHAR* argv[])
{
	TCHAR modulePath[MAX_PATH];

	// Gets the full filename of this exe
	GetModuleFileName(NULL, modulePath, MAX_PATH);

	// Trims the filename and appends a trailing directory slash
	std::wstring binDir(modulePath);
	binDir = binDir.substr(0, binDir.rfind(_T("\\"))) + _T("\\");

	std::wstring appPath(Opts.appPath);
	std::wstring workingDir(Opts.workingDir);
	appPath.insert(0, binDir);
	workingDir.insert(0, binDir);

	_TCHAR* space = _T(" ");

	for (int i = 1; i < argc; i++)
	{
		appPath.append(space);
		appPath.append(argv[i]);
	}
	size_t appPathLength = appPath.length() * sizeof(_TCHAR);
	_TCHAR* appPathFinal = new _TCHAR[appPathLength];
	_tcscpy_s(appPathFinal, appPathLength, appPath.c_str());

	SECURITY_ATTRIBUTES sa = { sizeof(SECURITY_ATTRIBUTES) };
	sa.nLength = sizeof(sa);
	sa.bInheritHandle = TRUE;
	sa.lpSecurityDescriptor = NULL;

	STARTUPINFOW si = { sizeof(STARTUPINFOW) };
	si.cb = sizeof(si);
	if (Opts.WaitForCompletion)
	{
		si.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
		si.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
		si.hStdError = GetStdHandle(STD_ERROR_HANDLE);
		si.dwFlags |= STARTF_USESTDHANDLES;
	}

	//_tprintf(_T("out is null: %d\n"), si.hStdOutput == NULL);
	//_tprintf(_T("err is null: %d\n"), si.hStdError == NULL);
	//_tprintf(_T("in is null: %d\n"), si.hStdInput == NULL);

	// TODO: error handling
	PROCESS_INFORMATION pi;
	CreateProcessW(NULL, appPathFinal, NULL, &sa, Opts.WaitForCompletion, 0, NULL, workingDir.c_str(), &si, &pi);
	if (Opts.WaitForCompletion)
	{
		WaitForSingleObject(pi.hProcess, INFINITE);
	}
	return 0;
}