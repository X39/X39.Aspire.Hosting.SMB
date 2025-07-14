using System.Net;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SMBLibrary;
using SMBLibrary.Client;
using X39.Util;

namespace X39.Aspire.Hosting.SMB;

/// <summary>
/// Represents a health check for an SMB connection.
/// Implements the <see cref="IHealthCheck" /> interface to perform
/// health checks on SMB shares by connecting, authenticating,
/// and verifying access based on the provided connection string.
/// </summary>
/// <param name="connectionString">The connection string to check.</param>
public sealed class SmbHealthCheck(string connectionString) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var uri = new Uri(connectionString);
        var username = uri.UserInfo
            .Split(':')[0]
            .Split('/')
            .First();
        var domain = uri.UserInfo
                         .Split(':')[0]
                         .Split('/')
                         .Reverse()
                         .Skip(1)
                         .FirstOrDefault()
                     ?? string.Empty;
        var password = uri.UserInfo
            .Split(':')[1];
        var host = uri.Host;
        var port = uri.Port;


        try
        {
            var client = CreateClient(2);
            using var connection = ConnectTo(client, host, port);
            using var login = Login(client, domain, username, password);
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
    }


    private static ISMBClient CreateClient(int protocolVersion)
    {
        ISMBClient client = protocolVersion switch
        {
            1 => new SMB1Client(),
            2 => new SMB2Client(),
            _ => throw new ArgumentOutOfRangeException(nameof(protocolVersion), protocolVersion, null),
        };
        return client;
    }

    private static IDisposable ConnectTo(ISMBClient client, string host, int port)
    {
        var ipAddress = ResolveToIpAddress(host);
        return new Disposable(
            () =>
            {
                // We have to resolve the corresponding method by hand because for some reason the library author decided against making this method public, preventing the port-scheme to work here
                var result = client switch
                {
                    SMB1Client smb1Client => ConnectToSmb1(smb1Client),
                    SMB2Client smb2Client => ConnectToSmb2(smb2Client),
                    _                     => throw new Exception("Unknown SMB client"),
                };

                // ReSharper disable once InvertIf
                if (result is false)
                {
                    throw new Exception($"Failed to connect to {ipAddress}:{port}");
                }
            },
            client.Disconnect
        );

        bool ConnectToSmb1(SMB1Client smbClient)
        {
            var methodInfo = typeof(SMB2Client).GetMethod(
                "Connect",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(IPAddress), typeof(SMBTransportType), typeof(int), typeof(bool), typeof(int)]
            );
            if (methodInfo is null)
                throw new Exception(
                    "Failed to find Connect method. The reflection lookup might be outdated. Please manually decompile (or check the sources) and update the reference where this exception occured."
                );
            var result = methodInfo.Invoke(
                smbClient,
                [
                    ipAddress,
                    SMBTransportType.DirectTCPTransport,
                    port,
                    true, // forceExtendedSecurity
                    SMB2Client.DefaultResponseTimeoutInMilliseconds,
                ]
            );
            return (bool) result!;
        }

        bool ConnectToSmb2(SMB2Client smbClient)
        {
            var methodInfo = typeof(SMB2Client).GetMethod(
                "Connect",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(IPAddress), typeof(SMBTransportType), typeof(int), typeof(int)]
            );
            if (methodInfo is null)
                throw new Exception(
                    "Failed to find Connect method. The reflection lookup might be outdated. Please manually decompile (or check the sources) and update the reference where this exception occured."
                );
            var result = methodInfo.Invoke(
                smbClient,
                [ipAddress, SMBTransportType.DirectTCPTransport, port, SMB2Client.DefaultResponseTimeoutInMilliseconds]
            );
            return (bool) result!;
        }

        static IPAddress ResolveToIpAddress(string host)
        {
            var hostAddresses = Dns.GetHostAddresses(host);
            if (hostAddresses.Length == 0)
                throw new Exception($"Cannot resolve host name {host} to an IP address");
            return hostAddresses[0];
        }
    }


    private static IDisposable Login(ISMBClient client, string userDomain, string userName, string password)
    {
        return new Disposable(
            () =>
            {
                var result = client.Login(userDomain, userName, password);
                // ReSharper disable once InvertIf
                if (result is not NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception($"Failed to login with {userName} at {userDomain} having status {result}");
                }
            },
            () => { client.Logoff(); }
        );
    }
}
