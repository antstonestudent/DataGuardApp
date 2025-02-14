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
}