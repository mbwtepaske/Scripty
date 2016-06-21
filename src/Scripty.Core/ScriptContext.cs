using System;
using System.IO;

using Scripty.Core.Output;
using Scripty.Core.ProjectModel;

namespace Scripty.Core
{
  public class ScriptContext : IDisposable
  {
    internal ScriptContext(string scriptFilePath, string projectFilePath, ProjectTree projectTree)
    {
      if (string.IsNullOrEmpty(scriptFilePath))
      {
        throw new ArgumentException("Value cannot be null or empty", nameof(scriptFilePath));
      }

      if (!Path.IsPathRooted(scriptFilePath))
      {
        throw new ArgumentException("The script file path must be absolute");
      }

      Output = new OutputFileCollection(scriptFilePath);
      ProjectFilePath = projectFilePath;
      ProjectTree = projectTree;
      ScriptFilePath = scriptFilePath;
    }

    public string Content
    {
      get;
      set;
    }

    public ScriptContext Context => this;

    public OutputFileCollection Output
    {
      get;
    }

    public string ProjectFilePath
    {
      get;
    }

    public ProjectTree ProjectTree
    {
      get;
    }

    public string ScriptFilePath
    {
      get;
    }

    public void Dispose() => Output.Dispose();
  }
}