using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MapleImgEditor;

internal sealed class WzFileLockInfo
{
	public int ProcessId { get; init; }

	public string ApplicationName { get; init; } = string.Empty;
}

internal static class WzFileLockInspector
{
	private const int ErrorMoreData = 234;

	public static IReadOnlyList<WzFileLockInfo> GetLockingProcesses(string path)
	{
		List<WzFileLockInfo> lockers = new List<WzFileLockInfo>();
		string sessionKey = Guid.NewGuid().ToString();
		if (RmStartSession(out uint session, 0, sessionKey) != 0)
		{
			return lockers;
		}
		try
		{
			if (RmRegisterResources(session, 1u, new string[1] { path }, 0u, null, 0u, null) != 0)
			{
				return lockers;
			}
			uint needed = 0u;
			uint count = 0u;
			int result = RmGetList(session, out needed, ref count, null, out uint _);
			if (result != ErrorMoreData)
			{
				return lockers;
			}
			count = needed;
			RmProcessInfo[] processes = new RmProcessInfo[count];
			result = RmGetList(session, out needed, ref count, processes, out uint _);
			if (result != 0)
			{
				return lockers;
			}
			for (int i = 0; i < count; i++)
			{
				int processId = (int)processes[i].Process.ProcessId;
				if (processId == Environment.ProcessId)
				{
					continue;
				}
				string applicationName = processes[i].AppName;
				if (string.IsNullOrWhiteSpace(applicationName))
				{
					applicationName = GetProcessName(processId);
				}
				lockers.Add(new WzFileLockInfo
				{
					ProcessId = processId,
					ApplicationName = applicationName
				});
			}
		}
		finally
		{
			RmEndSession(session);
		}
		return lockers;
	}

	private static string GetProcessName(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			return process.ProcessName;
		}
		catch
		{
			return "Unknown process";
		}
	}

	[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
	private static extern int RmStartSession(out uint sessionHandle, int sessionFlags, string sessionKey);

	[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
	private static extern int RmRegisterResources(uint sessionHandle, uint fileCount, string[]? fileNames, uint applicationCount, RmUniqueProcess[]? applications, uint serviceCount, string[]? serviceNames);

	[DllImport("rstrtmgr.dll")]
	private static extern int RmGetList(uint sessionHandle, out uint processInfoNeeded, ref uint processInfo, [In, Out] RmProcessInfo[]? affectedApplications, out uint rebootReasons);

	[DllImport("rstrtmgr.dll")]
	private static extern int RmEndSession(uint sessionHandle);

	[StructLayout(LayoutKind.Sequential)]
	private struct RmUniqueProcess
	{
		public uint ProcessId;

		public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct RmProcessInfo
	{
		public RmUniqueProcess Process;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string AppName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string ServiceName;

		public int ApplicationType;

		public uint AppStatus;

		public uint TSSessionId;

		[MarshalAs(UnmanagedType.Bool)]
		public bool Restartable;
	}
}
