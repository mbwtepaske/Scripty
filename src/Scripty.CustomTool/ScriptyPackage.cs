using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace Scripty
{
  /// <summary>
  /// Visual Studio Package of the Scripty extension.
  /// </summary>
  [InstalledProductRegistration("Scripty", "Scripty Extension", "1.0.0.0")]
  [Guid("0E562512-21AE-4BA0-807C-957C34C64D6F")]
  [PackageRegistration(UseManagedResourcesOnly = true)]

  // Auto-load the package for C# and Visual Basic projects to extend file properties 
  [ProvideAutoLoad(VSConstants.UICONTEXT.CSharpProject_string)]
  //[ProvideAutoLoad(VSConstants.UICONTEXT.VBProject_string)]
  internal class ScriptyPackage : Package
  {
    private ICollection<IDisposable> Registration
    {
      get;
    } = new List<IDisposable>();

    protected override void Initialize()
    {
      base.Initialize();

      Registration.Add(new BrowseObjectExtenderProvider(this, PrjBrowseObjectCATID.prjCATIDCSharpFileBrowseObject));
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        foreach (var registration in Registration)
        {
          registration.Dispose();
        }
      }

      base.Dispose(disposing);
    }
  }
}
