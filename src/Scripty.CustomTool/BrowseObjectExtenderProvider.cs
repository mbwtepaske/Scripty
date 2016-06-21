namespace Scripty
{
  using System;
  using EnvDTE;
  using Microsoft.VisualStudio.Shell.Interop;

  /// <summary>
  /// Provides a "Template" property extender for C# and Visual Basic project items.
  /// </summary>
  internal sealed class BrowseObjectExtenderProvider : IExtenderProvider, IDisposable
  {
    private const string ExtenderName = "Scripty";

    private readonly string _extenderCategory;
    private readonly ObjectExtenders _extenders;
    private readonly int _providerCookie;
    private readonly IServiceProvider _serviceProvider;

    public BrowseObjectExtenderProvider(IServiceProvider serviceProvider, string extenderCategory)
    {
      _serviceProvider = serviceProvider;
      _extenderCategory = extenderCategory;
      _extenders = (ObjectExtenders) serviceProvider.GetService(typeof(ObjectExtenders));
      _providerCookie = _extenders.RegisterExtenderProvider(extenderCategory, ExtenderName, this);
    }

    public bool CanExtend(string extenderCategory, string extenderName, object extendee) 
      => extenderCategory == _extenderCategory 
      && extenderName == ExtenderName 
      && extendee is IVsBrowseObject;

    public void Dispose() => _extenders.UnregisterExtenderProvider(_providerCookie);

    public object GetExtender(string extenderCategory, string extenderName, object extendee, IExtenderSite site, int extenderCookie) 
      => new BrowseObjectExtender(_serviceProvider, (IVsBrowseObject) extendee, site, extenderCookie);
  }
}
