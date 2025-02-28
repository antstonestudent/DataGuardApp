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

namespace DataGuardApp
{
    public partial class MainWindow : Window
    {
        private string selectedFile = string.Empty; // Function that stores the path of the selected file
        private string[] storedHashes;
        private string hashFilePath = "stored_hashes.txt"; // Path to store known hashes
        private bool columnsVisible = false; // Function that controls collapsible panel visibility

        // Constructor: Function that initializes the UI components and loads stored hashes
        public MainWindow()
        {
            InitializeComponent();
            LoadStoredHashes();
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
                FilePathTextBox.Text = selectedFile;
            }
        }

        // Function that computes the hash values (MD5, SHA-1, SHA-256, SHA-512) for the selected file
        private void ComputeHashButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFile))
            {
                MessageBox.Show("Please select a file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string md5Hash = MD5Hasher.ComputeHash(selectedFile);
            string sha1Hash = SHA1Hasher.ComputeHash(selectedFile);
            string sha256Hash = SHA256Hasher.ComputeHash(selectedFile);
            string sha512Hash = SHA512Hasher.ComputeHash(selectedFile);

            // Function that updates the visual indicators for each hash type
            UpdateIndicator(indicator1, md5Hash);
            UpdateIndicator(indicator2, sha1Hash);
            UpdateIndicator(indicator3, sha256Hash);
            UpdateIndicator(indicator4, sha512Hash);

            // Function that stores computed hashes
            SaveHash(md5Hash);
            SaveHash(sha1Hash);
            SaveHash(sha256Hash);
            SaveHash(sha512Hash);
        }

        // Function that updates the visual indicator based on hash verification
        private void UpdateIndicator(System.Windows.Shapes.Ellipse indicator, string computedHash)
        {
            if (string.IsNullOrWhiteSpace(OfficialHashTextBox.Text))
            {
                // No official hash provided, set indicator to yellow (unknown status)
                indicator.Fill = Brushes.Yellow;
            }
            else
            {
                string officialHash = OfficialHashTextBox.Text.Trim().ToLower();

                if (computedHash == officialHash)
                {
                    // Hash matches, set indicator to green (safe)
                    indicator.Fill = Brushes.Green;
                }
                else
                {
                    // Hash does not match, set indicator to red (potential tampering)
                    indicator.Fill = Brushes.Red;
                }
            }
        }

}
