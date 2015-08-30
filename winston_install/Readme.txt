*********************************** Elevate **********************************

Elevate is an official CubicleSoft product distributed as part of the MyUpdate
Toolkit package.  Direct support for this product is available.  See the
"License" section at the end of this document for the EULA.



Elevate is a package designed for software developers who are integrating
existing products into Windows Vista who use CreateProcess() and its variants.
If you are reading this, you have probably run into the problem with UAC and
ShellExecuteEx():  It is extremely limited in terms of functionality and
replacing existing CreateProcess() code that has been well-tested to use
ShellExecuteEx() can be either seriously annoying or impossible.

What this package offers:

Link_Create()
Link_CreateAsUser()
Link_CreateWithLogon()
Link_CreateWithToken()
Link_Destroy()
Link_CreateProcessA()
Link_CreateProcessW()
Link_ShellExecuteExA()
Link_ShellExecuteExW()
Link_ShellExecuteA()
Link_ShellExecuteW()
Link_LoadLibraryA()
Link_LoadLibraryW()
Link_SendData()
Link_GetData()
Link_SendFinalize()

CreateProcessElevatedA()
CreateProcessElevatedW()
CreateProcessAsUserElevatedA()
CreateProcessAsUserElevatedW()
CreateProcessWithLogonElevatedW()
CreateProcessWithTokenElevatedW()
SH_RegCreateKeyExElevatedA()
SH_RegCreateKeyExElevatedW()
SH_RegOpenKeyExElevatedA()
SH_RegOpenKeyExElevatedW()
SH_RegCloseKeyElevated()
ShellExecuteElevatedA()
ShellExecuteElevatedW()
ShellExecuteExElevatedA()
ShellExecuteExElevatedW()

IsUserAnAdmin()

The last function might seem kind of odd.  IsUserAnAdmin() is exported from
Shell32.dll but MSDN Library documentation says it could disappear.  It is
a useful function when adding a feature to existing code to force elevation of
a process (although, you should consider using a manifest instead).  To use
the Elevate package, you should do the following:

Result = CreateProcess(...ParameterList...);  // Existing line of code.
if (!Result && GetLastError() == ERROR_ELEVATION_REQUIRED)
{
  HMODULE LibHandle = LoadLibrary("Elevate.dll");
  if (LibHandle != NULL)
  {
    DLL_CreateProcessElevated = (typecast)GetProcAddress("CreateProcessElevatedA");
    if (DLL_CreateProcessElevated)
    {
      ...Custom handle changes here *...
      Result = DLL_CreateProcessElevated(...ParameterList...);
      ...Custom handle connections here *...
    }
    FreeLibrary(LibHandle);
  }
}
// Continue as usual with existing code...

(Above is C/C++ code, but Elevate works well with other languages too.
 VB, Delphi, C#, etc.)

NOTE:  ERROR_ELEVATION_REQUIRED = 740.  If it isn't #defined, you'll need to
       know that to #define it.

* The STARTUPINFO structure that gets passed into these CreateProcessElevated()
  functions is a bit different from what you might be used to.  If you use the
  STARTF_USESTDHANDLES flag (dwFlags), then the hStdInput, hStdOutput, and
  hStdError HANDLEs should actually be LPCSTR/LPCWSTR's representing the name
  of a named pipe to use.  If the address is INVALID_HANDLE_VALUE or NULL,
  NULL is passed on.  To pass the standard HANDLE, use an empty string.  To
  pass a named pipe, make sure the format is '\\[Servername]\pipe\[Pipename]'.

The Elevate.dll file will only load under Windows Vista and later.  You must
use LoadLibrary()/GetProcAddress() to access the functions in the DLL.  All
functions take the same parameters as input as their non-elevated counterparts
with the sole exception of the STARTUPINFO changes.  See MSDN Library for the
documentation on each non-elevated API should you need to refresh your memory.

If you are a C++ developer, export_types.h is included to aid you in your
quest.  This file contains a lot of 'typedef's to make it easy to gain access
to the functions in the DLL.

The basic Elevate.dll call such as CreateProcessElevated() elevates
Elevate.exe and then turns around and starts the desired process.  Each call
to the basic function set causes the UAC dialog to display each and every time
a basic function is called.

Behind the scenes, every basic call uses the Link_Create() calls.  A "Link" is
a permanent connection between Elevate.dll and Elevate.exe.  Establishing the
link causes the UAC dialog to display one time.  Once established, a HANDLE is
returned from the Link_Create() calls that can be used to start processes,
load DLLs into Elevate.exe (Link_LoadLibrary()), and send loaded DLLs
serialized information (Link_SendData(), Link_GetData(), and Link_SendFinalize()).
When the program is done, the Link HANDLE is destroyed with Link_Destroy() and
the Elevate.exe process exits.

Using a permanent link, it is possible to interact with DLLs that hook APIs in
the elevated process space, display dialogs and then return information across
the Link, execute Windows APIs without incurring the cost of starting another
executable, and much more.  Permanent links also allow multiple elevated
processes to be started and only display just one UAC dialog to the user.

There is one downside to using a permanent Link:  Links are not multithread-
safe.  If one Link is shared throughout a process across multiple threads, be
sure to use an appropriate synchronization mechanism.

Currently Elevate.dll defers IsUserAnAdmin() to Shell32.dll if it exists.  If
the other functions are added to Windows (e.g. via a Service Pack), you will
need an updated version of Elevate.dll to take advantage of whatever Windows
offers.

The only data NOT transported across to the destination EXE are the security
descriptors (lpProcessAttributes and lpThreadAttributes).  Well, that is not
entirely true.  If the security descriptor is not NULL, then a NULL security
descriptor is sent (i.e. lpSecurityDescriptor = NULL).  If NULL is passed in
for the security descriptor, then NULL is sent.  This covers the majority of
the cases.  Usually the most important part of a security descriptor is the
bInheritHandle variable.

The ShellExecute() functions are somewhat limited when using the "runas" verb.
Only one verb can be used at a time.  Some applications might need, for
example, the ability to use the "print" verb of an elevated application.  The
ShellExecuteElevated() functions are useful for this.

* When using ShellExecuteExElevated() and the 'hkeyClass' member of
  SHELLEXECUTEINFO is used, you must use the RegCreateKeyExElevated() or
  RegOpenKeyExElevated() functions to modify the HKEY.  These functions take
  the same parameters as RegCreateKeyEx() and RegOpenKeyEx() respectively.

If your application is a console application and you are running another
console application elevated, the Elevate package convienently ties the
elevated process to the existing console.  One of the nuisances of UAC is
opening a new console for an elevated process.  Integrating this package fixes
the problem.

When making a DLL, there are two functions the DLL must export:

  DWORD ElevatedLink_SendData(LPCSTR *Data, DWORD DataSize);
  int ElevatedLink_GetData(LPSTR *ResultData);

And optionally export a third function:

  void ElevatedLink_SendFinalize();

ElevatedLink_SendData is where serialized data comes in from a Link_SendData()
call.  It is up to you as to how serialization is done on both ends of the
Link.  What is returned from this function is the amount of memory to allocate
to store the serialized results.

ElevatedLink_GetData retrieves the serialized results from the
ElevatedLink_SendData() call.

ElevatedLink_SendFinalize lets your DLL clean up any resources that might
still be around after the GetData() call.  This allows for the scenario where
the caller might need to do something with the data before an API is called
that causes the data sent to become invalid.

Callers of Link_SendData() may not call Link_SendData() again until
Link_GetData() and Link_SendFinalize() have been called.  Doing so would cause
the application to deadlock.

The only downside to the Elevate package is the user doesn't know what
application is actually going to start.  It would be helpful if UAC told the
user what application was requesting the UAC elevation and show the trust
level of that application as well.

Vista-specific note:  The EXTENDED_STARTUPINFO_PRESENT flag is NOT supported.
If this flag is specified, it is stripped out of the call.

Due to the way UAC operates, the PROCESS_INFORMATION structure HANDLEs that
are returned only have SYNCHRONIZE access.  In certain cases, the HANDLEs may
be NULL even though the process/thread IDs will be non-zero.  When the process
is created in a suspended state, the thread HANDLE will attempt to be opened
with (SYNCHRONIZE | THREAD_SUSPEND_RESUME) access.


License
(C) 2007 CubicleSoft.  All Rights Reserved.
This product is free to use for any purpose (freeware, commercial software,
etc.).  By using this product, you agree to not hold CubicleSoft liable for
any damage it might cause.

Not that I personally think it will cause any damage, I'm just covering my
legal rear.  I'm providing this as a service to the software development
community.

If you feel the need to donate something because Elevate just saved you a ton
of time, I have a gigantic list of stuff I'd love to see arrive at my front
door:

http://www.cubiclesoft.com/AlternatePurchase.txt


Thomas Hruska
CubicleSoft President

http://www.cubiclesoft.com/
