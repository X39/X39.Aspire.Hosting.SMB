using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;

namespace X39.Aspire.Hosting.SMB;

/// <summary>
/// Provides extension methods for working with <see cref="IResourceBuilder{TResource}" /> for <see cref="SmbResource" /> instances.
/// </summary>
public static class ResourceExtensions
{
    /// <summary>
    /// Configures the <see cref="SmbResource" /> instance with a local folder, binding it to the container's storage path.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IResourceBuilder{TResource}" /> instance for configuring the <see cref="SmbResource" />.
    /// </param>
    /// <param name="path">
    /// The path to the folder with a base of <see cref="IDistributedApplicationBuilder.AppHostDirectory"/>
    /// to be used with the resource.
    /// Must point to an existing directory.
    /// </param>
    /// <returns>
    /// The updated <see cref="IResourceBuilder{TResource}" /> instance for further configuration.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="SmbResource" /> already has a folder configured.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown if the specified <paramref name="path" /> does not exist.
    /// </exception>
    public static IResourceBuilder<SmbResource> WithFolder(this IResourceBuilder<SmbResource> builder, string path)
    {
        if (builder.Resource.HasFolder)
            throw new InvalidOperationException("The resource already has a folder.");
        path = Path.GetFullPath(path, builder.ApplicationBuilder.AppHostDirectory);
        return WithExactFolder(builder, path);
    }

    /// <summary>
    /// Configures the <see cref="SmbResource" /> instance with a specific folder, binding it to the container's storage path without additional path resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IResourceBuilder{TResource}" /> instance for configuring the <see cref="SmbResource" />.
    /// </param>
    /// <param name="absolutePath">
    /// The absolute path to the folder to be used with the resource. The specified folder must exist.
    /// </param>
    /// <returns>
    /// The updated <see cref="IResourceBuilder{TResource}" /> instance for further configuration.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="SmbResource" /> already has a folder configured.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown if the specified <paramref name="absolutePath" /> does not exist.
    /// </exception>
    public static IResourceBuilder<SmbResource> WithExactFolder(this IResourceBuilder<SmbResource> builder, string absolutePath)
    {
        if (builder.Resource.HasFolder)
            throw new InvalidOperationException("The resource already has a folder.");
        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException(
                $"The directory '{absolutePath}' (full: '{Path.GetFullPath(absolutePath)}') does not exist."
            );
        builder.Resource.HasFolder = true;
        return builder.WithBindMount(absolutePath, "/storage");
    }
}
