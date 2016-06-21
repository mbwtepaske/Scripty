using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Scripting;

using Scripty.Core.ProjectModel;

namespace Scripty.Core
{
  public static class ScriptEngine
  {
    public static async Task<ScriptResult> Evaluate(string projectFilePath, string scriptPath, string content)
    {
      if (string.IsNullOrEmpty(projectFilePath))
      {
        throw new ArgumentException("Value cannot be null or empty.", nameof(projectFilePath));
      }

      if (!Path.IsPathRooted(projectFilePath))
      {
        throw new ArgumentException("Project path must be absolute", nameof(projectFilePath));
      }

      var projectTree = new ProjectTree(projectFilePath);
      var options = ScriptOptions.Default
        .WithFilePath(scriptPath)
        // Microsoft.CodeAnalysis.Workspaces
        .WithReferences(typeof(Microsoft.CodeAnalysis.Project).Assembly)
        // Microsoft.Build
        .WithReferences(typeof(Microsoft.Build.Evaluation.Project).Assembly)
        // Scripty.Core
        .WithReferences(typeof(ScriptEngine).Assembly)
        .WithImports("System")
        .WithImports("Scripty.Core")
        .WithImports("Scripty.Core.Output")
        .WithImports("Scripty.Core.ProjectModel");
      
      using (var context = new ScriptContext(scriptPath, projectFilePath, projectTree))
      {
        try
        {
          await CSharpScript.EvaluateAsync(File.ReadAllText(scriptPath), options, context);
        }
        catch (CompilationErrorException compilationError)
        {
          return new ScriptResult(context.Output.OutputFiles, compilationError
            .Diagnostics
            .Select(x => new ScriptError
            {
              Message = x.GetMessage(),
              Line = x.Location.GetLineSpan().StartLinePosition.Line,
              Column = x.Location.GetLineSpan().StartLinePosition.Character
            })
            .ToList());
        }
        catch (AggregateException aggregateException)
        {
          return new ScriptResult(context.Output.OutputFiles, aggregateException.InnerExceptions
            .Select(x => new ScriptError
            {
              Message = x.ToString()
            })
            .ToList());
        }
        catch (Exception ex)
        {
          return new ScriptResult(context.Output.OutputFiles, new[]
          {
            new ScriptError
            {
              Message = ex.ToString()
            }
          });
        }

        return new ScriptResult(context.Output.OutputFiles);
      }
    }
  }
}
