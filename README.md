# SharpSvc
SharpSvc is a simple code set to interact with the SC Manager API using the same DCERPC process as sc.exe, which open with TCP port 135 and is followed by the use of an ephemeral TCP port. This code is compatible with Cobalt Strike.

```
C:\>SharpSvc.exe

[-] Usage:
        --ListSvc <Computer|local|hostname|ip> <State|all|running|stopped>
        --GetSvc <Computer|local|hostname|ip> <ServiceName|RemoteRegistry> <Function|list|stop|start|enable|disable>
        --AddSvc <Computer|local|hostname|ip> <Name|MyCustomService> <DisplayName|"My Custom Service"> <ExecutablePath|C:\Windows\notepad.exe + Args>
        --RemoveSvc <Computer|local|hostname|ip> <ServiceName|MyCustomService>
```

#### Enable and validate a remote service.
```
C:\>SharpSvc.exe --GetSvc 10.10.10.10 RemoteRegistry enable

The RemoteRegistry service mode is currently set to Disabled
Enabling the RemoteRegistry service...
The RemoteRegistry service status is now set to StartPending

C:\>SharpSvc.exe --GetSvc 10.10.10.10 RemoteRegistry list

        ServiceName: RemoteRegistry
        DisplayName: Remote Registry
        MachineName: 10.10.10.10
        ServiceType: Win32ShareProcess
        StartType: Automatic
        Status: Running
```

#### Disable and validate a remote service.
```
C:\>SharpSvc.exe --GetSvc 10.10.10.10 RemoteRegistry disable

The RemoteRegistry service mode is currently set to Automatic
Disabling the RemoteRegistry service...
The RemoteRegistry service status is now set to StopPending

C:\>SharpSvc.exe --GetSvc 10.10.10.10 RemoteRegistry list

        ServiceName: RemoteRegistry
        DisplayName: Remote Registry
        MachineName: 10.10.10.10
        ServiceType: Win32ShareProcess
        StartType: Disabled
        Status: Stopped

```

#### Create, validate, and delete a remote service.
```
C:\>SharpSvc.exe --AddSvc 10.10.10.10 MyCustomService "My Custom Service" C:\Windows\notepad.exe

The MyCustomService service was successfully created.

C:\>SharpSvc.exe --GetSvc 10.10.10.10 MyCustomService list

        ServiceName: MyCustomService
        DisplayName: My Custom Service
        MachineName: 10.10.10.10
        ServiceType: Win32OwnProcess
        StartType: Automatic
        Status: Stopped

C:\>SharpSvc.exe --RemoveSvc 10.10.10.10 MyCustomService

The MyCustomService service was successfully deleted.

```
