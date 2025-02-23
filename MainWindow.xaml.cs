using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace DataGuardApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Event Handler for the Minimize Button
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // Minimize the window
        this.WindowState = WindowState.Minimized;
    }

    // Event Handler for the Close Button
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the window
        this.Close();
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    // Event for the collapsible instructions panel
    private bool columnsVisible = false; // This is to set the visibility of the text to collapsed by default
    private void Polygon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (columnsVisible)
        {
            // Hide the left and right columns by setting their visibility to collapsed
            LeftText.Visibility = Visibility.Collapsed;
            RightText.Visibility = Visibility.Collapsed;
        }
        else
        {
            //Show the columns restoring their visibility
            LeftText.Visibility = Visibility.Visible;
            RightText.Visibility = Visibility.Visible;
        }
        columnsVisible = !columnsVisible;
    }

    private string selectedFile = string.Empty; // Store the selected file path for later usage

    private void FileButton_Click(object sender, RoutedEventArgs e) // To open file explorer and select file
    {
        var openFileDialog = new OpenFileDialog
        {
            DefaultExt = ".exe", // Default extension
            Filter = "Text Documents (*.txt)|*.txt|All Files (*.*)|*.*" // Filter can be adjusted
        };

        bool? result = openFileDialog.ShowDialog();
        if (result == true)
        {
            selectedFile = openFileDialog.FileName;
        }
    }

    // DragEnter event handler
    private void FileButton_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    // Drop event hanlder - processes the dropped file
    private void FileButton_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Retrieve the array of dropped files
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                if (files.Length > 1)
                {
                    MessageBox.Show("Please only selected one file at a time.", "Multiple Files Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedFile = files[0];
            }
        }
    }

}