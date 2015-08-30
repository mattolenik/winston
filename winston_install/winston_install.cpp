#include "stdafx.h"
//------------------------------------------------------------------------------
// This file contains simple implementation showing how a chainer can launch
// the .NET 4.5 Setup.exe with /pipe command line and uses the MmioChainer class
// to listen for progress.
//------------------------------------------------------------------------------

#include "MmioChainer.h"
#include "IProgressObserver.h"
#include "winston_tar.h"

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

class Server : public ChainerSample::MmioChainer, public ChainerSample::IProgressObserver
{
public:
	// Mmio chainer will create section with given name. You should make this and the event name unique.
	// Event is also created by the Mmio chainer and name is saved in the mapped data structure.
	Server() :ChainerSample::MmioChainer(L"winston-install-net46", L"winston-install-event") //customize for your event names
	{}

	bool Launch(const CString& args)
	{
		CString cmdline = L"NDP46-KB3045560-Web.exe /pipe winston-install-net46 " + args; // Customize with name and location of setup .exe that you want to run
		STARTUPINFO si = { 0 };
		si.cb = sizeof(si);
		PROCESS_INFORMATION pi = { 0 };

		// Launch the Setup.exe which installs the .NET 4.5 Framework
		BOOL bLaunchedSetup = ::CreateProcess(NULL,
			cmdline.GetBuffer(),
			NULL, NULL, FALSE, 0, NULL, NULL,
			&si,
			&pi);

		// If successful 
		if (bLaunchedSetup != 0)
		{
			IProgressObserver& observer = dynamic_cast<IProgressObserver&>(*this);
			Run(pi.hProcess, observer);

			DWORD dwResult = GetResult();
			if (E_PENDING == dwResult)
			{
				::GetExitCodeProcess(pi.hProcess, &dwResult);
			}

			printf("Result: %08X\n  ", dwResult);

			// Get internal result
			// If the failure is in a MSI/MSP payload, the internal result refers to the error messages
			// http://msdn.microsoft.com/en-us/library/aa372835(VS.85).aspx
			HRESULT hrInternalResult = GetInternalResult();
			printf("Internal result: %08X\n", hrInternalResult);




			::CloseHandle(pi.hThread);
			::CloseHandle(pi.hProcess);
		}
		else
		{
			printf("CreateProcess failed");
			ReportLastError();
		}

		return (bLaunchedSetup != 0);
	}

private: // IProgressObserver
	virtual void OnProgress(unsigned char ubProgressSoFar)
	{
		printf("Progress: %i\n  ", ubProgressSoFar);

		// Testing: BEGIN - To test Abort behavior, uncomment the folllowing code.
		//if (ubProgressSoFar > 127)
		//{
		//    printf("\rDeliberately Aborting with progress at %i  ", ubProgressSoFar);
		//    Abort();
		//}
		// Testing END
	}

	virtual void Finished(HRESULT hr)
	{
		// This HRESULT is communicated over MMIO and may be different than process
		// exit code of the Chainee Setup.exe itself.
		printf("\r\nFinished HRESULT: 0x%08X\r\n", hr);
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
	virtual DWORD Send(DWORD dwMessage, LPVOID pData, DWORD dwDataLength)
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
	void ReportLastError(void)
	{
		DWORD dwLastError = 0;
		LPWSTR lpstrMsgBuf = NULL;
		DWORD dwMessageLength = 0;

		dwLastError = GetLastError();
		dwMessageLength = FormatMessageW(
			FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
			NULL,
			dwLastError,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
			(LPTSTR)&lpstrMsgBuf,
			0,
			NULL);

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

// Main entry point for program
int __cdecl wmain(int argc, wchar_t *argv[], wchar_t *envp[])
{
	CString args = "/q /norestart";

	auto result = Server().Launch(args);
	auto tar = winston_tar;
	auto data = reinterpret_cast<const char*>(tar);

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	wchar_t tmp[512];
	GetTempPath(512, tmp);
	auto tmpName = std::wstring(L"winston-install-") + std::to_wstring(rand() % 10000);
	wchar_t tmpOut[600];
	PathCombine(tmpOut, tmp, tmpName.c_str());
	CreateDirectory(tmpOut, NULL);

	size_t c = 0;
	while(true)
	{

		auto rec = &data[c];
		std::string name(rec);
		int size = octal_string_to_int(&rec[124], 11);
		c += 512;
		if (c + 512 >= winston_tar_length)
		{
			break;
		}
		wchar_t path[1024];
		std::wstring wName = converter.from_bytes(name);
		PathCombine(path, tmpOut, wName.c_str());

		switch (rec[156])
		{
		case '0':
		case '\0':
			//auto file = &data[c];
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
			CreateDirectory(path, NULL);
			break;
		}
		wprintf(L"%s\n", path);
		if (size > 0)
		{
			c += (((size - 1) / 512) + 1) * 512;
		}
	}


	STARTUPINFO si = { 0 };
	si.cb = sizeof(si);
	PROCESS_INFORMATION pi = { 0 };

	wchar_t cmdline[1024];
	PathCombine(cmdline, tmpOut, L"winston.cmd");
	std::wstring installCmd(cmdline);

	std::wstring qt(L"\"");
	std::wstring fullCmd = qt + cmdline + qt + std::wstring(L" selfinstall");
	std::vector<wchar_t> buf(fullCmd.begin(), fullCmd.end());
	buf.push_back(0);

	BOOL installSuccess = ::CreateProcess(NULL,
		buf.data(),
		NULL, NULL, FALSE, 0, NULL,
		tmpOut,
		&si,
		&pi);


	RemoveDirectory(tmpOut);
	DeleteFile(tmpOut);
	return 0;
}