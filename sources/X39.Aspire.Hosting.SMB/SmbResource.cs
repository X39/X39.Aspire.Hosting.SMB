using Aspire.Hosting.ApplicationModel;

namespace X39.Aspire.Hosting.SMB;

/// <summary>
/// Represents an SMB resource that can be used in distributed application hosting scenarios.
/// </summary>
/// <remarks>
/// This class extends <see cref="ContainerResource" /> and implements <see cref="IResourceWithConnectionString" />.
/// The SMB resource is designed to handle SMB shares with configurable name, username, password, and share details.
/// </remarks>
public sealed class SmbResource(string name, string username, string password, string share) : ContainerResource(name),
    IResourceWithConnectionString
{
    /// <summary>
    /// Represents the name of the primary endpoint protocol used for establishing the connection, typically "tcp".
    /// </summary>
    public const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Valkey server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Represents an expression evaluating to the connection string of the SMB resource.
    /// </summary>
    /// <remarks>
    /// The connection string is constructed dynamically based on the SMB resource's configuration, including the username, password,
    /// primary endpoint properties such as host and port, and the share information.
    /// </remarks>
    /// <returns>
    /// An instance of <see cref="ReferenceExpression" /> representing the SMB resource's connection string in the format:
    /// smb://{username}:{password}@{host}:{port}/{share}.
    /// </returns>
    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create(
            $"smb://{username}:{password}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}/{share}"
        );

    internal bool HasFolder { get; set; }
}
