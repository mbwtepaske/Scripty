using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Scripty.Core;
using Scripty.Core.Output;

namespace Scripty
{
  [ComVisible(true)]
  [CodeGeneratorRegistration(typeof(ScriptyGenerator), "C# Scripty Generator", VSConstants.UICONTEXT.CSharpProject_string, GeneratesDesignTimeSource = true)]
  [CodeGeneratorRegistration(typeof(ScriptyGenerator), "VB.NET Scripty Generator", VSConstants.UICONTEXT.VBProject_string, GeneratesDesignTimeSource = true)]
  [Guid("1B8589A2-58FF-4413-9EA3-A66A1605F1E4")]
  [ProvideObject(typeof(ScriptyGenerator))]
  public class ScriptyGenerator : BaseCodeGeneratorWithSite
  {
    // The name of this generator (use for 'Custom Tool' property of project item) ReSharper disable once InconsistentNaming
    internal static string Name = "Scripty";

    protected override string GetDefaultExtension() => ".log";

    /// <summary>
    /// Function that builds the contents of the generated file based on the contents of the input file.
    /// </summary>
    /// <param name="inputFileContent">Content of the input file</param>
    /// <returns>Generated file as a byte array</returns>
    protected override byte[] GenerateCode(string inputFileContent)
    {
      var projectItem = GetProjectItem();

      var template = projectItem.Properties.Item("Template")?.Value as string;

      if (!String.IsNullOrWhiteSpace(template) && !File.Exists(template))
      {
        ActivityLog.LogError(Name, $"Project item meta-data 'Scripty Template' is assigned to '{template}', but the file does not exist.");

        return null;
      }

      var fullPath = (string) projectItem.Properties.Item("FullPath")?.Value;

      var inputFilePath = fullPath;
      var project = projectItem.ContainingProject;
      var solution = projectItem.DTE.Solution;

      // Run the generator and get the results
      //var source = new ScriptSource(template ?? fullPath, String.IsNullOrWhiteSpace(template) ? inputFileContent : File.ReadAllText(template));
      var result = ScriptEngine.Evaluate(project.FullName, template ?? fullPath, template != null ? File.ReadAllText(fullPath) : null).Result;

      // Report errors
      if (result.Errors.Count > 0)
      {
        foreach (var error in result.Errors)
        {
          GeneratorError(4, error.Message, (uint) error.Line, (uint) error.Column);
        }

        return null;
      }

      // Add generated files to the project
      foreach (var outputFile in result.OutputFiles.Where(x => x.BuildAction != BuildAction.GenerateOnly))
      {
        var outputItem = projectItem.ProjectItems
          .Cast<ProjectItem>()
          .FirstOrDefault(x => x.Properties.Item("FullPath")?.Value?.ToString() == outputFile.FilePath)
          ?? projectItem.ProjectItems.AddFromFile(outputFile.FilePath);

        outputItem.Properties.Item("ItemType").Value = outputFile.BuildAction.ToString();
      }

      // Remove/delete files from the last generation but not in this one
      var logPath = Path.ChangeExtension(inputFilePath, ".log");

      if (File.Exists(logPath))
      {
        var logLines = File.ReadAllLines(logPath);

        foreach (var fileToRemove in logLines.Where(x => result.OutputFiles.All(y => y.FilePath != x)))
        {
          solution.FindProjectItem(fileToRemove)?.Delete();
        }
      }

      return Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, result.OutputFiles.Select(x => x.FilePath)));
    }
  }
}
