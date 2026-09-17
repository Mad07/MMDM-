namespace Mark1
{
    /// <summary>
    /// Marker type only - anchors Resources/SharedResource.resx and Resources/SharedResource.es.resx
    /// for IStringLocalizer&lt;SharedResource&gt;. Deliberately kept outside the Mark1.Resources
    /// namespace: ASP.NET Core's resource-path convention combines ResourcesPath with the type's
    /// namespace-minus-root, so a type namespaced under Resources would double up the folder name.
    /// </summary>
    public class SharedResource
    {
    }
}
