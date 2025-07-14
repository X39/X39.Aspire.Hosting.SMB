using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace X39.Aspire.Hosting.SMB;

/// <summary>
/// Provides extension methods for configuring and managing SMB (Server Message Block) resources
/// within a distributed application using the <see cref="IDistributedApplicationBuilder" />.
/// </summary>
public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds an SMB share resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">
    /// The instance of <see cref="IDistributedApplicationBuilder" /> to which the SMB share resource will be added.
    /// </param>
    /// <param name="name">
    /// The name of the SMB resource. This value cannot be null or whitespace.
    /// </param>
    /// <param name="port">
    /// The optional network port to use for the SMB service. Defaults to <see langword="null" />, falling back to the default port 445.
    /// </param>
    /// <param name="shareName">
    /// The name of the SMB share. Defaults to "SHARE". This value cannot be null or whitespace.
    /// </param>
    /// <param name="username">
    /// The username for accessing the SMB share. Defaults to "ANON". This value cannot be null or whitespace.
    /// </param>
    /// <param name="password">
    /// The password for accessing the SMB share. Defaults to "ANON". This value cannot be null or whitespace.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IResourceBuilder{SmbResource}" /> for further configuration and chaining operations.
    /// </returns>
    public static IResourceBuilder<SmbResource> AddSmbShare(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string shareName = "SHARE",
        string username = "ANON",
        string password = "ANON"
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shareName);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var resource = new SmbResource(name, username, password, shareName);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(
            resource,
            async (_, ct) =>
            {
                connectionString = await resource.ConnectionStringExpression
                    .GetValueAsync(ct)
                    .ConfigureAwait(false);

                if (connectionString is null)
                    throw new DistributedApplicationException(
                        $"ConnectionStringAvailableEvent was published for the '{resource.Name}' resource but the connection string was null."
                    );
            }
        );

        var healthCheckKey = $"{name}_healthcheck";
        builder.Services
            .AddHealthChecks()
            .Add(
                new HealthCheckRegistration(
                    healthCheckKey,
                    _ => new SmbHealthCheck(
                        connectionString ?? throw new InvalidOperationException("Connection string is unavailable")
                    ),
                    HealthStatus.Unhealthy,
                    default,
                    default
                )
            );

        return builder.AddResource(resource)
            .WithEndpoint(port, 445, "smb", SmbResource.PrimaryEndpointName)
            .WithEnvironment("USER", username)
            .WithEnvironment("PASS", password)
            .WithEnvironment("NAME", shareName)
            .WithImage("dockurr/samba", "4.21")
            .WithImageRegistry("docker.io")
            .WithHealthCheck(healthCheckKey);
    }
}
