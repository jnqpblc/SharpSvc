using System;
using System.IO;
using System.ServiceProcess;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace SharpSvc
{
	class SharpSvc
	{
		static void Main(string[] args)
		{
			if (args == null || args.Length < 1)
			{
				printUsage();
			}

			if ((args[0].ToUpper() == "--LISTSVC") && (args.Length == 3))
			{
				string Computer = args[1];
				string State = args[2];
				ListSvc(Computer, State);
			}
			else if ((args[0].ToUpper() == "--GETSVC") && (args.Length == 4))
			{
				string Computer = args[1];
				string ServiceName = args[2];
				string Function = args[3];
				GetSvc(Computer, ServiceName, Function);
			}
			else if ((args[0].ToUpper() == "--ADDSVC") && ((args.Length == 5) || (args.Length == 6)))
			{
				string Computer = args[1];
				string ServiceName = args[2];
				string DisplayName = args[3];
				string BinaryPathName = args[4];
				uint ServiceType = 0;
				if ((args.Length == 5) || ((args.Length == 6) && (args[5].ToUpper() == "WIN32OWNPROCESS")))
				{
					ServiceType = SERVICE_WIN32_OWN_PROCESS;
				}
				else if (args[5].ToUpper() == "KERNELDRIVER")
				{
					ServiceType = SERVICE_KERNEL_DRIVER;
				}
				if (ServiceType != 0)
				{
                    AddSvc(Computer, ServiceName, DisplayName, BinaryPathName, ServiceType);
                }
				else
				{
					printUsage();
				}
            }
			else if ((args[0].ToUpper() == "--REMOVESVC") && (args.Length == 3))
			{
				string Computer = args[1];
				string ServiceName = args[2];
				RemoveSvc(Computer, ServiceName);
			}
			else
			{
				printUsage();
			}
		}

		static void printUsage()
		{
			Console.WriteLine("\n[-] Usage: \n\t--ListSvc <Computer|local|hostname|ip> <State|all|running|stopped>" +
				"\n\t--GetSvc <Computer|local|hostname|ip> <ServiceName|Spooler> <Function|list|stop|start|enable|disable>" +
				"\n\t--AddSvc <Computer|local|hostname|ip> <Name|MyCustomService> <DisplayName|\"My Custom Service\">" +
				" <ExecutablePath|C:\\Windows\\notepad.exe + Args> <ServiceType|win32ownprocess|kerneldriver>" +
				"\n\t--RemoveSvc <Computer|local|hostname|ip> <ServiceName|MyCustomService>\n");
			System.Environment.Exit(1);
		}

		static void ListSvc(string Computer, string State)
		{
			ServiceController[] scServices;
			if (Computer.ToUpper() == "LOCAL")
			{
				Computer = ".";
			}
			scServices = ServiceController.GetServices(Computer);
			List<string> ServiceList = new List<string>();
			foreach (ServiceController scTemp in scServices)
			{
				if (State.ToUpper() == "ALL")
				{
					ServiceList.Add("\t" + scTemp.DisplayName + "," + scTemp.ServiceName + "," + scTemp.StartType);
				}
				else if (State.ToUpper() == "RUNNING")
				{
					if (scTemp.Status == ServiceControllerStatus.Running)
					{
						ServiceList.Add("\t" + scTemp.DisplayName + "," + scTemp.ServiceName + "," + scTemp.StartType);
					}
				}
				else if (State.ToUpper() == "STOPPED")
				{
					if (scTemp.Status == ServiceControllerStatus.Stopped)
					{
						ServiceList.Add("\t" + scTemp.DisplayName + "," + scTemp.ServiceName + "," + scTemp.StartType);
					}
				}
				else
				{
					printUsage();
				}
			}
			ServiceList.Sort();
			foreach (string Entry in ServiceList)
			{
				Console.WriteLine(Entry);
			}
		}
		static void GetSvc(string Computer, string ServiceName, string Function)
		{
			if (Computer.ToUpper() == "LOCAL")
			{
				Computer = ".";
			}
			ServiceController sc = new ServiceController(ServiceName, Computer);
			if (Function.ToUpper() == "LIST")
			{
				Console.WriteLine("\n\tServiceName: {0}\n\tDisplayName: {1}\n\tMachineName: {2}\n\tServiceType: {3}\n\tStartType: {4}\n\tStatus: {5}\n", sc.ServiceName, sc.DisplayName, sc.MachineName, sc.ServiceType, sc.StartType, sc.Status);
			}
			else if (Function.ToUpper() == "START")
			{
				Console.WriteLine("\nThe {0} service status is currently set to {1}", sc.ServiceName, sc.Status);
				if (sc.Status == ServiceControllerStatus.Stopped)
				{
					Console.WriteLine("Starting the {0} service...", sc.ServiceName);
					try
					{
						sc.Start();
						sc.WaitForStatus(ServiceControllerStatus.Running);

						Console.WriteLine("The {0} service status is now set to {1}", sc.ServiceName, sc.Status);

					}
					catch (Exception e)
					{
						Console.WriteLine("Could not start the service, error: " + e.Message);
					}
				}
			}
			else if (Function.ToUpper() == "STOP")
			{
				Console.WriteLine("\nThe {0} service status is currently set to {1}", sc.ServiceName, sc.Status);
				if (sc.Status == ServiceControllerStatus.Running)
				{
					Console.WriteLine("Stopping the {0} service...", sc.ServiceName);
					try
					{
						sc.Stop();
						sc.WaitForStatus(ServiceControllerStatus.Stopped);

						Console.WriteLine("The {0} service status is now set to {1}", sc.ServiceName, sc.Status);

					}
					catch (Exception e)
					{
						Console.WriteLine("Could not stop the service, error: " + e.Message);
						return;
					}
				}
			}
			else if (Function.ToUpper() == "ENABLE")
			{
				Console.WriteLine("\nThe {0} service mode is currently set to {1}", sc.ServiceName, sc.StartType);
				if (sc.StartType != ServiceStartMode.Automatic)
				{
					Console.WriteLine("Enabling the {0} service...", sc.ServiceName);
					try
					{
						IntPtr scmHandle = GetHandleToSCM(Computer, sc.ServiceName);
						IntPtr serviceHandle = GetHandleToService(scmHandle, sc.ServiceName);
						bool changeServiceSuccess = ChangeServiceConfig(serviceHandle, SERVICE_NO_CHANGE, (uint)ServiceStartupType.Automatic, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null);
						if (!changeServiceSuccess)
						{
							string msg = $"Failed to update service configuration for service '{sc.ServiceName}'. ChangeServiceConfig returned error {Marshal.GetLastWin32Error()}.";
							throw new Exception(msg);
						}
						sc.Start();
					}
					catch (Exception e)
					{
						Console.WriteLine("Could not enable the service, error: " + e.Message);
						return;
					}

					Console.WriteLine("The {0} service status is now set to {1}", sc.ServiceName, sc.Status);
				}
			}
			else if (Function.ToUpper() == "DISABLE")
			{
				Console.WriteLine("\nThe {0} service mode is currently set to {1}", sc.ServiceName, sc.StartType);
				if (sc.StartType != ServiceStartMode.Disabled)
				{
					Console.WriteLine("Disabling the {0} service...", sc.ServiceName);
					try
					{
						IntPtr scmHandle = GetHandleToSCM(Computer, sc.ServiceName);
						IntPtr serviceHandle = GetHandleToService(scmHandle, sc.ServiceName);
						bool changeServiceSuccess = ChangeServiceConfig(serviceHandle, SERVICE_NO_CHANGE, (uint)ServiceStartupType.Disabled, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null);
						if (!changeServiceSuccess)
						{
							string msg = $"Failed to update service configuration for service '{sc.ServiceName}'. ChangeServiceConfig returned error {Marshal.GetLastWin32Error()}.";
							throw new Exception(msg);
						}
						sc.Stop();
					}
					catch (Exception e)
					{
						Console.WriteLine("Could not disable the service, error: " + e.Message);
						return;
					}

					Console.WriteLine("The {0} service status is now set to {1}", sc.ServiceName, sc.Status);
				}
			}
			else
			{
				printUsage();
			}
		}

		static void AddSvc(string Computer, string ServiceName, string DisplayName, string BinaryPathName, uint ServiceType)
		{
			if (Computer.ToUpper() == "LOCAL")
			{
				Computer = null;
			}
			try
			{
				IntPtr scmHandle = GetHandleToSCM(Computer, ServiceName);
				bool changeServiceSuccess = CreateService(scmHandle, ServiceName, DisplayName, (uint)SERVICE_ACCESS.SERVICE_ALL_ACCESS, ServiceType, (uint)ServiceStartupType.Automatic, SERVICE_ERROR_IGNORE, BinaryPathName, null, IntPtr.Zero, null, null, null);
				if (!changeServiceSuccess)
				{
					string msg = $"\nFailed to create the service configuration for service '{ServiceName}'. CreateService returned error {Marshal.GetLastWin32Error()}.";
					throw new Exception(msg);
				}
				else
				{
					Console.WriteLine("\nThe {0} service was successfully created.", ServiceName);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("\n{0}: {1}", e.GetType().Name, e.Message);
				return;
			}
		}
		static void RemoveSvc(string Computer, string ServiceName)
		{
			if (Computer.ToUpper() == "LOCAL")
			{
				Computer = null;
			}
			try
			{
				IntPtr scmHandle = GetHandleToSCM(Computer, ServiceName);
				IntPtr serviceHandle = GetHandleToService(scmHandle, ServiceName);
				bool changeServiceSuccess = DeleteService(serviceHandle);
				if (!changeServiceSuccess)
				{
					string msg = $"\nFailed to delete the service configuration for service '{ServiceName}'. DeleteService returned error {Marshal.GetLastWin32Error()}.";
					throw new Exception(msg);
				}
				else
				{
					Console.WriteLine("\nThe {0} service was successfully deleted.", ServiceName);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("\n{0}: {1}", e.GetType().Name, e.Message);
				return;
			}
		}

		static IntPtr GetHandleToSCM(string Computer, string ServiceName)
		{
			IntPtr scmHandle = OpenSCManager(Computer, null, (uint)SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
			if (scmHandle == IntPtr.Zero)
			{
				throw new Exception("\nFailed to obtain a handle to the service control manager database.");
			}

			return scmHandle;
		}
		static IntPtr GetHandleToService(IntPtr scmHandle, string ServiceName)
		{
			IntPtr serviceHandle = OpenService(scmHandle, ServiceName, (uint)SERVICE_ACCESS.SERVICE_ALL_ACCESS);
			if (serviceHandle == IntPtr.Zero)
			{
				throw new Exception($"\nFailed to obtain a handle to service '{ServiceName}'.");
			}

			return serviceHandle;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool ChangeServiceConfig(
			IntPtr hService,
			uint nServiceType,
			uint nStartType,
			uint nErrorControl,
			string lpBinaryPathName,
			string lpLoadOrderGroup,
			IntPtr lpdwTagId,
			[In] char[] lpDependencies,
			string lpServiceStartName,
			string lpPassword,
			string lpDisplayName);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool CreateService(
		  IntPtr hSCManager,
		  string lpServiceName,
		  string lpDisplayName,
		  uint dwDesiredAccess,
		  uint dwServiceType,
		  uint dwStartType,
		  uint dwErrorControl,
		  string lpBinaryPathName,
		  string lpLoadOrderGroup,
		  IntPtr lpdwTagId,
		  string lpDependencies,
		  string lpServiceStartName,
		  string lpPassword);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool DeleteService(IntPtr hService);

		[DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
		private static extern int CloseServiceHandle(IntPtr hSCObject);

		private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
		private const uint SERVICE_KERNEL_DRIVER = 0x00000001;
		private const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
		private const uint SERVICE_ERROR_IGNORE = 0x00000000;

		[Flags]
		public enum SCM_ACCESS : uint
		{
			STANDARD_RIGHTS_REQUIRED = 0xF0000,
			SC_MANAGER_CONNECT = 0x00001,
			SC_MANAGER_CREATE_SERVICE = 0x00002,
			SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
			SC_MANAGER_LOCK = 0x00008,
			SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
			SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
			SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
				SC_MANAGER_CONNECT |
				SC_MANAGER_CREATE_SERVICE |
				SC_MANAGER_ENUMERATE_SERVICE |
				SC_MANAGER_LOCK |
				SC_MANAGER_QUERY_LOCK_STATUS |
				SC_MANAGER_MODIFY_BOOT_CONFIG
		}

		[Flags]
		public enum SERVICE_ACCESS : uint
		{
			STANDARD_RIGHTS_REQUIRED = 0xF0000,
			SERVICE_QUERY_CONFIG = 0x00001,
			SERVICE_CHANGE_CONFIG = 0x00002,
			SERVICE_QUERY_STATUS = 0x00004,
			SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
			SERVICE_START = 0x00010,
			SERVICE_STOP = 0x00020,
			SERVICE_PAUSE_CONTINUE = 0x00040,
			SERVICE_INTERROGATE = 0x00080,
			SERVICE_USER_DEFINED_CONTROL = 0x00100,
			SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
				SERVICE_QUERY_CONFIG |
				SERVICE_CHANGE_CONFIG |
				SERVICE_QUERY_STATUS |
				SERVICE_ENUMERATE_DEPENDENTS |
				SERVICE_START |
				SERVICE_STOP |
				SERVICE_PAUSE_CONTINUE |
				SERVICE_INTERROGATE |
				SERVICE_USER_DEFINED_CONTROL)
		}

		public enum ServiceStartupType : uint
		{
			/// <summary>
			/// A device driver started by the system loader. This value is valid only for driver services.
			/// </summary>
			BootStart = 0,

			/// <summary>
			/// A device driver started by the IoInitSystem function. This value is valid only for driver services.
			/// </summary>
			SystemStart = 1,

			/// <summary>
			/// A service started automatically by the service control manager during system startup.
			/// </summary>
			Automatic = 2,

			/// <summary>
			/// A service started by the service control manager when a process calls the StartService function.
			/// </summary>
			Manual = 3,

			/// <summary>
			/// A service that cannot be started. Attempts to start the service result in the error code ERROR_SERVICE_DISABLED.
			/// </summary>
			Disabled = 4
		}
	}
}
