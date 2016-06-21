namespace Scripty
{
  using System;
  using System.ComponentModel;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  using System.Runtime.InteropServices;
  using EnvDTE;
  using Microsoft.VisualStudio;
  using Microsoft.VisualStudio.Shell.Interop;

  /// <summary>
  /// Adds "Custom Tool Template" properties to the C# and Visual Basic file properties.
  /// </summary>
  [ClassInterface(ClassInterfaceType.AutoDispatch)]
  [ComVisible(true)]
  [SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable", Justification = "Instances of this type are created only by TemplatePropertyProvider.")]
  public class BrowseObjectExtender
  {
    private readonly int _cookie;
    private readonly uint _item;
    private ProjectItem _projectItem;
    private readonly IVsHierarchy _hierarchy;
    private readonly IVsBuildPropertyStorage _propertyStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly IExtenderSite _site;

    internal BrowseObjectExtender(IServiceProvider serviceProvider, IVsBrowseObject browseObject, IExtenderSite site, int cookie)
    {
      Debug.Assert(serviceProvider != null, "serviceProvider");
      Debug.Assert(browseObject != null, "browseObject");
      Debug.Assert(site != null, "site");
      Debug.Assert(cookie != 0, "cookie");

      ErrorHandler.ThrowOnFailure(browseObject.GetProjectItem(out _hierarchy, out _item));

      _cookie = cookie;
      _propertyStorage = (IVsBuildPropertyStorage) _hierarchy;
      _serviceProvider = serviceProvider;
      _site = site;
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// This method notifies the extender site that the extender has been deleted in order to prevent Visual Studio 
    /// from crashing with an Access Violation exception.
    /// </remarks>
    ~BrowseObjectExtender()
    {
      try
      {
        _site.NotifyDelete(_cookie);
      }
      catch (InvalidComObjectException)
      {
        // This exception occurs when the Runtime-Callable Wrapper (RCW) was already disconnected from the COM object.
        // This typically happens when the extender is disposed when Visual Studio shuts down.
      }
    }
    
    /// <summary>
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Scripty Template")]
    //[Description("A T4 template used by the " + TemplatedFileGenerator.Name + " to generate code from this file.")]
    //[Editor(typeof(CustomToolTemplateEditor), typeof(UITypeEditor))]
    public string Template
    {
      get
      {
        string value;

        if (ErrorHandler.Failed(_propertyStorage.GetItemAttribute(_item, nameof(Template), out value)))
        {
          value = string.Empty;
        }

        return value;
      }
      set
      {
        //if (!string.IsNullOrWhiteSpace(value))
        //{
        //  if (!string.IsNullOrWhiteSpace((string) ProjectItem.Properties.Item("CustomTool").Value) &&
        //      TemplatedFileGenerator.Name != (string) ProjectItem.Properties.Item(ProjectItemProperty.CustomTool).Value)
        //  {
        //    throw new InvalidOperationException(
        //      string.Format(
        //        CultureInfo.CurrentCulture,
        //        "The '{0}' property is supported only by the {1}. Set the 'Custom Tool' property first.",
        //        TemplatePropertyDisplayName,
        //        TemplatedFileGenerator.Name));
        //  }

        //  // Report an error if the template cannot be found
        //  string fullPath = value;
        //  var templateLocator = (TemplateLocator) _serviceProvider.GetService(typeof(TemplateLocator));
        //  if (!templateLocator.LocateTemplate(ProjectItem.FileNames[1], ref fullPath))
        //  {
        //    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Template '{0}' could not be found", value));
        //  }
        //}

        ErrorHandler.ThrowOnFailure(_propertyStorage.SetItemAttribute(_item, "Template", value));

        // If the file does not have a custom tool yet, assume that by specifying the template user wants to use the T4Toolbox.TemplatedFileGenerator.
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace((string) ProjectItem.Properties.Item("Custom Tool").Value))
        {
          ProjectItem.Properties.Item("Custom Tool").Value = ScriptyGenerator.Name;
        }
      }
    }

    private ProjectItem ProjectItem
    {
      get
      {
        if (_projectItem == null)
        {
          object extObject;
          ErrorHandler.ThrowOnFailure(_hierarchy.GetProperty(_item, (int) __VSHPROPID.VSHPROPID_ExtObject, out extObject));
          _projectItem = (ProjectItem) extObject;
        }

        return _projectItem;
      }
    }
  }
}