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

    #region Properties


    private string _excludedProjects = string.Empty;
    public string ExcludedProjects {
      get => _excludedProjects;
      set => SetProperty(ref _excludedProjects, value);
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
      string excludedProjects = ExcludedProjects;
      bool exists = Directory.Exists(folderPath);
      if (!exists) {
        return;
      }
      HashSet<string> excludedProjectNames = new HashSet<string>();
      if (!string.IsNullOrWhiteSpace(excludedProjects)) {
        excludedProjectNames = excludedProjects.Split(",").ToHashSet();
      }

      const string projectEnding = ".csproj";
      Regex projectRefRegex = new Regex("[ ]*<ProjectReference[ ]*Include=\"[.\\a-zA-Z0-9]*\"[ ]*\\/>{1}");
      Regex inculdeRegex = new Regex("\"[.\\a-zA-Z0-9]*\"");
      StringBuilder sb = new StringBuilder();
      var allProjectFiles = Directory.EnumerateFiles(folderPath, $"*{projectEnding}", SearchOption.AllDirectories);
      foreach (var file in allProjectFiles) {
        FileInfo fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) {
          continue;
        }
        if (excludedProjectNames.Contains(fileInfo.Name)) {
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
            if (excludedProjectNames.Contains(referencingProjectName)) {
              continue;
            }
            sb.AppendLine(GetMappingString(fileInfo.Name?.Replace($"{projectEnding}", "") ?? "", referencingProjectName));
          }
          
        }
      }
      ResultText = sb.ToString();
    }

    /// <summary>Clear</summary>
    private void Clear() {
      ExcludedProjects = "";
      FolderPath = "";
      ResultText = "";
    }

    private void OpenFolder() {
      System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog();
      folderDlg.SelectedPath = FolderPath;
      if (folderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        FolderPath = folderDlg.SelectedPath;
      }
    }

    private string GetMappingString(string projectName, string referencingProjectName) {
      return $"[{projectName}] -> [{referencingProjectName}]";
    }

  }
}
