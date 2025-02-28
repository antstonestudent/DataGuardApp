using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using DataGuardApp.hashing;

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

        // Function that loads stored hashes from a file, or creates an empty file if it doesn't exist
        private void LoadStoredHashes()
        {
            if (File.Exists(hashFilePath))
            {
                storedHashes = File.ReadAllLines(hashFilePath);
            }
            else
            {
                storedHashes = new string[0];
                File.WriteAllText(hashFilePath, "");
            }
        }

        // Function that minimizes the window when the minimize button is clicked
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Function  that closes the application when the close button is clicked
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Function that enables window dragging when the user clicks and holds the mouse button
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Function that toggles the visibility of the left and right text panels when a polygon is clicked
        private void Polygon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LeftText.Visibility = columnsVisible ? Visibility.Collapsed : Visibility.Visible;
            RightText.Visibility = columnsVisible ? Visibility.Collapsed : Visibility.Visible;
            columnsVisible = !columnsVisible;
        }

        // Function that opens a file dialog for selecting a file and updates the file path text box
        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = "*.*",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFile = openFileDialog.FileName;
                FilePathTextBox.Text = selectedFile;
            }
        }

        // Function that llows the file button to accept dragged files
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

        // Function that handles file drop event and updates the file path
        private void FileButton_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1)
                {
                    MessageBox.Show("Please select only one file at a time.", "Multiple Files Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // Function that saves a computed hash to the stored hashes file, avoiding duplicates
        private void SaveHash(string newHash)
        {
            if (!storedHashes.Contains(newHash))
            {
                File.AppendAllText(hashFilePath, newHash + Environment.NewLine);
                storedHashes = File.ReadAllLines(hashFilePath);
            }
        }
    }
}
