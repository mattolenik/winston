#include "stdafx.h"
#include "MmioChainer.h"
#include "IProgressObserver.h"
#include "export_types.h"
#include "winston_tar.bin.h"
#include "elevate.bin.h"
#include "Prereqs.h"
#include "DownloadStatus.h"
#include "TempDirectory.h"
#include "winston_install.h"

/*
The classes:

MmioChainerBase
---------------
| |  MmioChainerBase class manages the communication and synchronization data
| |  datastructures. It also implements common Getters (for chainer) and
| |  Setters(for chainee).
| |
| |     MmioChainer
| +----------------
|         |     Creates Mmio Section and waits (Run()) for Chainee exe to exit or Chainee to be Aborted.
|         |     This can monitor progress and can cancel Chainee by sending Abort() message.
|         |   Server
|         +---------
|                   This code runs in the Chainer process. This object Constructs the MmioChainer object
|                   Launches the Chainee Setup.exe and waits for it to Exit.
|
|       MmioChainee
+-------------------
		  |    Opens up the Mmio section created by MmioChainer and uses that to communicate with Chainer.
		  |
		  |
		  |   MmioController
		  +---------
					Runs in the context of the chainee process, implements  ProgressObserver
					methods like Abort(), Finished(). The Chainee communicates with Chainer using these
					class methods.
*/

BOOL CreateProcessElevatedIfNeeded(
	LPCTSTR               lpApplicationName,
	LPTSTR                lpCommandLine,
	LPSECURITY_ATTRIBUTES lpProcessAttributes,
	LPSECURITY_ATTRIBUTES lpThreadAttributes,
	BOOL                  bInheritHandles,
	DWORD                 dwCreationFlags,
	LPVOID                lpEnvironment,
	LPCTSTR               lpCurrentDirectory,
	LPSTARTUPINFO         lpStartupInfo,
	LPPROCESS_INFORMATION lpProcessInformation,
	LPCWSTR elevateDll)
{
	BOOL result = CreateProcess(
		lpApplicationName,
		lpCommandLine,
		lpProcessAttributes,
		lpThreadAttributes,
		bInheritHandles,
		dwCreationFlags,
		lpEnvironment,
		lpCurrentDirectory,
		lpStartupInfo,
		lpProcessInformation);
	if (!result && GetLastError() == ERROR_ELEVATION_REQUIRED)
	{
		HMODULE libHandle = LoadLibrary(elevateDll);
		auto createProcessElevated = reinterpret_cast<DLL_CreateProcessElevatedWType>(GetProcAddress(libHandle, "CreateProcessElevatedW"));
		result = createProcessElevated(
			lpApplicationName,
			lpCommandLine,
			lpProcessAttributes,
			lpThreadAttributes,
			bInheritHandles,
			dwCreationFlags,
			lpEnvironment,
			lpCurrentDirectory,
			lpStartupInfo,
			lpProcessInformation);
	}
	return result;
}

BOOL CreateProcessElevatedIfNeeded(
	LPTSTR lpCommandLine,
	LPCTSTR lpCurrentDirectory,
	LPSTARTUPINFO& lpStartupInfo, // out
	LPPROCESS_INFORMATION& lpProcessInformation, // out
	LPCWSTR elevateDll)
{
	lpStartupInfo = new STARTUPINFO();
	lpStartupInfo->cb = sizeof(STARTUPINFO);
	lpProcessInformation = new PROCESS_INFORMATION();
	return CreateProcessElevatedIfNeeded(
		nullptr,
		lpCommandLine,
		nullptr,
		nullptr,
		FALSE,
		0,
		nullptr,
		lpCurrentDirectory,
		lpStartupInfo,
		lpProcessInformation,
		elevateDll);
}

// From https://msdn.microsoft.com/en-us/library/ff859983.aspx
class Server : public ChainerSample::MmioChainer, public ChainerSample::IProgressObserver
{
public:
	// Mmio chainer will create section with given name. You should make this and the event name unique.
	// Event is also created by the Mmio chainer and name is saved in the mapped data structure.
	Server() :ChainerSample::MmioChainer(L"winston-install-net46", L"winston-install-event")
	{}

	BOOL Launch(const std::wstring& args, const std::wstring& exe, const std::wstring& workingDir, const std::wstring& elevateDll)
	{
		std::wstring cmdline = exe + L" /pipe winston-install-net46 " + args;
		LPSTARTUPINFO si;
		LPPROCESS_INFORMATION pi;
		BOOL setupSuccess = CreateProcessElevatedIfNeeded(
			const_cast<LPWSTR>(cmdline.c_str()),
			workingDir.c_str(),
			si,
			pi,
			elevateDll.c_str());

		// If successful 
		if (setupSuccess)
		{
			IProgressObserver& observer = dynamic_cast<IProgressObserver&>(*this);
			Run(pi->hProcess, observer);

			DWORD dwResult = GetResult();
			if (E_PENDING == dwResult)
			{
				GetExitCodeProcess(pi->hProcess, &dwResult);
			}

			printf("Result: %08X\n  ", dwResult);

			// Get internal result
			// If the failure is in a MSI/MSP payload, the internal result refers to the error messages
			// http://msdn.microsoft.com/en-us/library/aa372835(VS.85).aspx
			HRESULT hrInternalResult = GetInternalResult();
			printf("Internal result: %08X\n", hrInternalResult);

			CloseHandle(pi->hThread);
			CloseHandle(pi->hProcess);
		}
		else
		{
			printf("CreateProcess failed");
			ReportLastError();
		}

		return setupSuccess;
	}

private: // IProgressObserver
	int lastLength = 0;
	int lastValue = -1;
	int lastSpin = -1;

	virtual void OnProgress(unsigned char ubProgressSoFar) override
	{
		if (lastValue == -1)
		{
            print(L"Progress: ");
		}
		if (lastSpin >= 0)
		{
            print(L'\b');
		}
		auto value = static_cast<int>(ceil(ubProgressSoFar / 255.0 * 100.0));
		if (value != lastValue)
		{
			lastValue = value;
			auto percent = std::to_wstring(value);
			if (lastLength > 0)
			{
                print(std::wstring(lastLength, L'\b'));
			}
			lastLength = percent.size();
            print(percent);
		}

		switch (lastSpin)
		{
		case -1:
		case 0:
            print(L'-');
			break;
		case 1:
            print(L'\\');
			break;
		case 2:
            print(L'|');
			break;
		case 3:
            print(L'/');
			break;
		}
		lastSpin++;
		if (lastSpin > 3) lastSpin = 0;


		// Testing: BEGIN - To test Abort behavior, uncomment the folllowing code.
		//if (ubProgressSoFar > 127)
		//{
		//    printf("\rDeliberately Aborting with progress at %i  ", ubProgressSoFar);
		//    Abort();
		//}
		// Testing END
	}

	virtual void Finished(HRESULT hr) override
	{
		// This HRESULT is communicated over MMIO and may be different than process
		// exit code of the Chainee Setup.exe itself.
		//printf("\r\nFinished HRESULT: 0x%08X\r\n", hr);
	}
	//------------------------------------------------------------------------------
		// SendMessage
		//
		// Sends a message and wait for the response.
		// dwMessage : Message to send
		// pData : The buffer to copy the data to
		// dwDataLength : Initially a pointer to the size of pBuffer.  Upon successful
		//                 call, the number of bytes copied to pBuffer.
		//------------------------------------------------------------------------------
	virtual DWORD Send(DWORD dwMessage, LPVOID pData, DWORD dwDataLength) override
	{
		DWORD dwResult = 0;
		printf("recieved message: %d\n", dwMessage);
		// Handle message
		switch (dwMessage)
		{
		case MMIO_CLOSE_APPS:
			dwResult = IDYES;  // Close apps
			break;
		default:
			break;
		}
		printf("  response: %d\n  ", dwResult);
		return dwResult;
	}

private:
	// Utility function to get text version of last error
	static void ReportLastError(void)
	{
		DWORD dwLastError = 0;
		LPWSTR lpstrMsgBuf = nullptr;
		DWORD dwMessageLength = 0;

		dwLastError = GetLastError();
		dwMessageLength = FormatMessageW(
			FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
			nullptr,
			dwLastError,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
			lpstrMsgBuf,
			0,
			nullptr);

		if (dwMessageLength)
		{
			// Display the string
			printf("Last error: %ls", lpstrMsgBuf);
			// Free the buffer.
			LocalFree(lpstrMsgBuf);
		}
	}
};

int octal_string_to_int(const char *current_char, unsigned int size)
{
	unsigned int output = 0;
	while (size > 0) {
		output = output * 8 + *current_char - '0';
		current_char++;
		size--;
	}
	return output;
}

inline std::wstring uniqifier()
{
	auto uniq = std::to_wstring(rand() % 999999999 + 1000000000);
	return uniq;
}

std::wstring extractFile(const std::wstring& directory, const std::wstring& filename, unsigned char* file, size_t length)
{
	auto fn = directory + std::wstring(L"\\") + filename;
	std::fstream f(fn, std::fstream::out | std::fstream::binary);
	f.write(reinterpret_cast<const char*>(file), length);
	return fn;
}

void downloadFile(LPCWSTR url, LPCWSTR filename, DownloadStatus* status)
{
	URLDownloadToFile(nullptr, url, filename, 0, status);
}

BOOL NetFxBootstrap(const TempDirectory& prereqs, const std::wstring& elevateDll)
{
	if (IsNetfx46Installed())
	{
		return true;
	}
	auto netFxInstall = prereqs.Path() + std::wstring(L"\\NDP46-KB3045560-Web.exe");
    println(L"Downloading .NET 4.6");
	DownloadStatus status;
	//downloadFile(L"https://download.microsoft.com/download/1/4/A/14A6C422-0D3C-4811-A31F-5EF91A83C368/NDP46-KB3045560-Web.exe", netFxInstall.c_str(), &status);
	downloadFile(L"https://download.microsoft.com/download/3/5/9/35980F81-60F4-4DE3-88FC-8F962B97253B/NDP461-KB3102438-Web.exe", netFxInstall.c_str(), &status);
    println("");
	std::wstring args = L"/q /norestart /ChainingPackage Winston";
    println(L"Installing .NET 4.6");
	auto result = Server().Launch(args, netFxInstall, prereqs.Path(), elevateDll);
    print(L"result ");
    println(result);
	return result;
}

DWORD VCRedistx86Bootstrap(const TempDirectory& prereqs, const std::wstring& elevateDll)
{
	if(IsVCRedist2015x86Installed())
	{
		return 0;
	}
	auto vcredistX86 = prereqs.Path() + std::wstring(L"\\vc_redist.x86.exe");
    println(L"Downloading Visual C++ 2015 redistributable (x86)");
	DownloadStatus status;
	downloadFile(L"https://download.microsoft.com/download/C/E/5/CE514EAE-78A8-4381-86E8-29108D78DBD4/VC_redist.x86.exe", vcredistX86.c_str(), &status);
    println("");
	std::wstring cmdline = vcredistX86 + L" /install /quiet /norestart";
    println(L"Installing Visual C++ 2015 redistributable (x86)");
	LPSTARTUPINFO si;
	LPPROCESS_INFORMATION pi;
	BOOL success = CreateProcessElevatedIfNeeded(const_cast<LPWSTR>(cmdline.c_str()), prereqs.PathCStr(), si, pi, elevateDll.c_str());
	if(!success)
	{
		return GetLastError();
	}

	WaitForSingleObject(pi->hProcess, INFINITE);
	DWORD result;
	GetExitCodeProcess(pi->hProcess, &result);
	CloseHandle(pi->hThread);
	CloseHandle(pi->hProcess);
    println(L"Finished");
	return result;
}

bool hasArg(int argc, wchar_t* argv[], wchar_t* arg)
{
    for (int i = 0; i < argc; i++)
    {
        if (lstrcmpi(argv[i], arg) == 0)
        {
            return true;
        }
    }
    return false;
}

// Main entry point for program
int __cdecl wmain(int argc, wchar_t *argv[], wchar_t *envp[])
{
    quiet = hasArg(argc, argv, L"/q");
    println(logo);
	srand(static_cast<unsigned int>(time(nullptr)));
	wchar_t tmp[512];
	GetTempPath(512, tmp);
	std::wstring tmpDir(tmp);
	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	TempDirectory installSource(tmpDir + std::wstring(L"\\winston_install_") + uniqifier());
	TempDirectory prereqs(tmpDir + std::wstring(L"\\winston_prereqs_") + uniqifier());
	auto elevateDll = extractFile(prereqs.Path(), std::wstring(L"elevate.dll"), elevate_dll, elevate_dll_length);
	auto elevateExe = extractFile(prereqs.Path(), std::wstring(L"elevate.exe"), elevate_exe, elevate_exe_length);

	auto netfxSuccess = NetFxBootstrap(prereqs, elevateDll);
	if (!netfxSuccess)
	{
		return 1;
	}

	//DWORD vcX86result = VCRedistx86Bootstrap(prereqs, elevateDll);
	//println(L"Result: ");

	auto tar = winston_tar;
	auto data = reinterpret_cast<const char*>(tar);

	size_t c = 0;
	while (true)
	{

		auto rec = &data[c];
		std::string name(rec);
		int size = octal_string_to_int(&rec[124], 11);
		c += 512;
		if (c + 512 >= winston_tar_length)
		{
			break;
		}
		std::wstring path = installSource.Path() + std::wstring(L"\\") + converter.from_bytes(name);

		switch (rec[156])
		{
		case '0':
		case '\0':
			// normal file
		{
			std::fstream file(path, std::fstream::out | std::fstream::binary);
			file.write(&data[c], size);
		}
		break;
		case '1':
			// hard link
			break;
		case '2':
			// symbolic link
			break;
		case '5':
			// directory
			CreateDirectory(path.c_str(), nullptr);
			break;
		}
		if (size > 0)
		{
			c += (((size - 1) / 512) + 1) * 512;
		}
	}

	STARTUPINFO si = { 0 };
	si.cb = sizeof(si);
	PROCESS_INFORMATION pi = { 0 };

	std::wstring qt(L"\"");
	std::wstring cmdline = installSource.Path() + std::wstring(L"\\winston.exe");
	std::wstring fullCmd = qt + cmdline + qt + std::wstring(L" selfinstall");
	std::vector<wchar_t> buf(fullCmd.begin(), fullCmd.end());
	buf.push_back(0);

	BOOL installSuccess = CreateProcess(
		nullptr,
		buf.data(),
		nullptr,
		nullptr,
		FALSE,
		0,
		nullptr,
		installSource.PathCStr(),
		&si,
		&pi);

    if (installSuccess && !quiet)
    {
        // Sleep so the window doesn't disappear so fast that it isn't seen and user thinks something is wrong
        Sleep(3000);
    }

	WaitForSingleObject(pi.hProcess, INFINITE);
	CloseHandle(pi.hThread);
	CloseHandle(pi.hProcess);

	return installSuccess ? 0 : 1;
}