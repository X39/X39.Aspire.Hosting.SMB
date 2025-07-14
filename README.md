# X39.Aspire.Hosting.SMB library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a SMB resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire SMB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package X39.Aspire.Hosting.SMB
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a SMB resource and consume the connection using the following methods:

```csharp
var smbShare = builder.AddSmbShare("default-smb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(smbShare);
```

## Usage note for Windows
Windows has some problems with accessing the SMB shares due to the Windows Explorer not supporting any port but 445 for SMB.
There is no solution for that yet in the context of this library.

## Advanced Configuration

### Configuring a SMB Share

The `AddSmbShare` method provides several parameters to customize your SMB resource:

```csharp
var smbShare = builder.AddSmbShare(
    name: "custom-smb",       // Resource name (required)
    port: 445,                // Custom port (optional, defaults to random)
    shareName: "MY_SHARE",    // Share name (optional, defaults to "SHARE")
    username: "user1",        // Username (optional, defaults to "ANON")
    password: "securepass"    // Password (optional, defaults to "ANON")
);
```

### Binding a Local Folder

You can bind a local folder to the SMB share using the `WithFolder` extension method:

```csharp
var smbShare = builder.AddSmbShare("data-share")
                     .WithFolder("/path/to/local/folder");
```

This mounts the specified local directory to the `/storage` path in the container, making it accessible through the SMB share.

## Connection String Format

The SMB resource provides a connection string in the following format:

```
smb://{username}:{password}@{host}:{port}/{share}
```

This connection string is automatically passed to services that reference the SMB resource.

## Health Checks

The library automatically configures health checks for the SMB resource, ensuring your application can monitor the availability of the SMB share.

## Container Information

The SMB resource is containerized using the `dockurr/samba:4.21` Docker image.

## Available Extension Methods

- `AddSmbShare` - Adds an SMB share resource to your distributed application
- `WithFolder` - Binds a local folder to the SMB share

## Resource Properties

The `SmbResource` class provides the following properties:

- `PrimaryEndpoint` - Access to the primary TCP endpoint of the SMB server
- `ConnectionStringExpression` - Expression that evaluates to the connection string

## Feedback & contributing

https://github.com/X39/X39.Aspire.Hosting.SMB
