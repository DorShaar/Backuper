using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BackupServiceInstaller;

public class WindowsServiceHandle : IDisposable
{
	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern void CloseServiceHandle(IntPtr SCHANDLE);
	
	public IntPtr Handle { get; }

	public WindowsServiceHandle(IntPtr handler)
	{
		Handle = validatePtr(handler);
	}

	public void Dispose()
	{
		if (Handle != IntPtr.Zero)
		{
			CloseServiceHandle(Handle);
		}
	}
	
	private static IntPtr validatePtr(IntPtr handler)
	{
		if (handler != IntPtr.Zero)
		{
			return handler;
		}
		
		int errorCode = Marshal.GetLastWin32Error();
		throw new Win32Exception(errorCode);
	}
}