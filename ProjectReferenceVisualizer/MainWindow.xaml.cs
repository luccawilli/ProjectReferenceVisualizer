using System.Windows;

namespace ProjectReferenceVisualizer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {

    public MainWindow() {
      VisualizerViewModel viewModel = new VisualizerViewModel();
      InitializeComponent();
      DataContext = viewModel;
    }


  }
}