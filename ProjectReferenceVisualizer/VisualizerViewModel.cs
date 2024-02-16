using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProjectReferenceVisualizer {
  public class VisualizerViewModel : Binding.BindableBase {

    private static String envVariable = "ProjectReferenceVisualizer";

    public VisualizerViewModel() {

      String? folder = Environment.GetEnvironmentVariable(envVariable, EnvironmentVariableTarget.User);
      if (!String.IsNullOrEmpty(folder)) {
        FolderPath = folder;
      }
    }

    #region Properties

    private string _inculdedProject = string.Empty;
    public string InculdedProject {
      get => _inculdedProject;
      set => SetProperty(ref _inculdedProject, value);
    }

    private string _folderPath = string.Empty;
    public string FolderPath {
      get => _folderPath;
      set => SetProperty(ref _folderPath, value);
    }

    private string _resultText = string.Empty;
    public string ResultText {
      get => _resultText;
      set => SetProperty(ref _resultText, value);
    }

    private ICommand _startCommand = null!;
    /// <summary></summary>
    public ICommand StartCommand {
      get {
        if (_startCommand == null) {
          _startCommand = new Binding.RelayCommand(
              param => Start()
          );
        }
        return _startCommand;
      }
    }

    private ICommand _clearCommand = null!;
    /// <summary></summary>
    public ICommand ClearCommand {
      get {
        if (_clearCommand == null) {
          _clearCommand = new Binding.RelayCommand(
              param => Clear()
          );
        }
        return _clearCommand;
      }
    }

    private ICommand _openFolderCommand = null!;
    /// <summary></summary>
    public ICommand OpenFolderCommand {
      get {
        if (_openFolderCommand == null) {
          _openFolderCommand = new Binding.RelayCommand(
              param => OpenFolder()
          );
        }
        return _openFolderCommand;
      }
    }

    #endregion

    /// <summary>Start</summary>
    private void Start() {
      string folderPath = FolderPath;
      string includedProject = InculdedProject;
      bool exists = Directory.Exists(folderPath);
      if (!exists) {
        ResultText = "Ordner exisitert nicht.";
        return;
      }

      const string projectEnding = ".csproj";
      Regex projectRefRegex = new Regex("[ ]*<ProjectReference[ ]*Include=\"[.\\a-zA-Z0-9]*\"[ ]*\\/>{1}");
      Regex inculdeRegex = new Regex("\"[.\\a-zA-Z0-9]*\"");
      var allProjectFiles = Directory.EnumerateFiles(folderPath, $"*{projectEnding}", SearchOption.AllDirectories);

      Dictionary<string, List<string>> references = new Dictionary<string, List<string>>();
      foreach (var file in allProjectFiles) {
        FileInfo fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) {
          continue;
        }
        var fileContent = File.ReadAllText(file);
        var projectRefMatches = projectRefRegex.Matches(fileContent);
        foreach (Match match in projectRefMatches) {
          var projectRefStrings = match.Value?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];          
          foreach(var projectRefString in projectRefStrings) {
            if (string.IsNullOrEmpty(projectRefString)) {
              continue;
            }
            var referencingProjectName = inculdeRegex.Match(projectRefString).Value?.Split("\\")?.Where(x => x.EndsWith(projectEnding + "\""))?.FirstOrDefault()?.Replace($"{projectEnding}\"", "");
            if (string.IsNullOrEmpty(referencingProjectName)) {
              continue;
            }

            string mappingString = fileInfo.Name?.Replace($"{projectEnding}", "") ?? "";
            if (!references.ContainsKey(mappingString)) {
              references[mappingString] = new List<string>();
            }
            references[mappingString].Add(referencingProjectName);
          }          
        }
      }

      StringBuilder sb = new StringBuilder();
      if (references.ContainsKey(includedProject)) {
        var refs = references[includedProject];
        List<Tuple<string, string>> transitiveRef = new List<Tuple<string, string>>();
        GetTransitiveRefernces(refs, references, transitiveRef);
        refs.ForEach(x => sb.AppendLine(GetMappingString(includedProject, x)));
        transitiveRef.Distinct().ToList().ForEach(x => sb.AppendLine(GetMappingString(x.Item1, x.Item2)));
      }
      ResultText = sb.ToString();
    }

    /// <summary>Clear</summary>
    private void Clear() {
      InculdedProject = "";
      FolderPath = "";
      ResultText = "";
    }

    private async Task OpenFolder() {
      System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog();
      folderDlg.SelectedPath = FolderPath;
      if (folderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        FolderPath = folderDlg.SelectedPath;
        await Task.Run(() => Environment.SetEnvironmentVariable(envVariable, FolderPath, EnvironmentVariableTarget.User));
      }
    }

    private string GetMappingString(string projectName, string referencingProjectName) {
      return $"[{projectName}] -> [{referencingProjectName}]";
    }

    private void GetTransitiveRefernces(List<string> transitiveRefs, Dictionary<string, List<string>> references, List<Tuple<string, string>> result) {
      foreach (string transitiveRef in transitiveRefs) {
        if (!references.ContainsKey(transitiveRef)) {
          continue;
        }
        references[transitiveRef].ForEach(x => result.Add(new Tuple<string, string>(transitiveRef, x)));
        GetTransitiveRefernces(references[transitiveRef], references, result);
      }
    }

  }
}
