using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;

namespace DataGuardApp
{
    public partial class MainWindow : Window
    {
        private string selectedFile = string.Empty; // Function that stores the path of the selected file
        private string hashStatus = "NotComputed";
        private bool columnsVisible = false; // Function that controls collapsible panel visibility
        private string md5Hash = string.Empty;
        private string sha1Hash = string.Empty;
        private string sha256Hash = string.Empty;
        private string sha512Hash = string.Empty;

        // Constructor: Function that initializes the UI components and loads stored hashes
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

        // Event handler to allow user to drag the screen around to reposition the window
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Event for the collapsible instructions panel
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

        private void FileButton_Click(object sender, RoutedEventArgs e) // To open file explorer and select file
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".bat", // Default extension
                Filter = "All Files (*.*)|*.*" // Filter can be adjusted
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                selectedFile = openFileDialog.FileName;
                DiscoverHashesPS();
                UpdateStatusIndicators();
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

        // Drop event handler - processes the dropped file
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
                    DiscoverHashesPS();
                    UpdateStatusIndicators();
                }
            }
        }

        private Dictionary<string, string> LoadReferenceHashes()
        {
            // Adjust the relative path as needed (this example assumes the file is in a folder named "testfile" at the project level)
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testfile", "TestHashes.txt");
            var referenceHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Reference hash file not found at: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return referenceHashes;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                // Expecting format: "Algorithm: hash"
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string algorithm = parts[0].Trim();
                    string expectedHash = parts[1].Trim().ToLowerInvariant();
                    referenceHashes[algorithm] = expectedHash;
                }
            }

            return referenceHashes;
        }

        // Discover hashes of selectedFile using silent PowerShell
        private void DiscoverHashesPS()
        {
            // Check that a valid file has been selected
            if (string.IsNullOrEmpty(selectedFile) || !File.Exists(selectedFile))
            {
                hashStatus = "No file selected";
                return;
            }

            // Construct a PowerShell command that outputs the four hash values on separate lines
            string psCommand =  $"& {{ " +
                                $"Write-Output (Get-FileHash -Path '{selectedFile}' -Algorithm MD5).Hash; " +
                                $"Write-Output (Get-FileHash -Path '{selectedFile}' -Algorithm SHA1).Hash; " +
                                $"Write-Output (Get-FileHash -Path '{selectedFile}' -Algorithm SHA256).Hash; " +
                                $"Write-Output (Get-FileHash -Path '{selectedFile}' -Algorithm SHA512).Hash; " +
                                $"}}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{psCommand}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                Process? process = Process.Start(psi);
                if (process == null)
                {
                    hashStatus = "Error";
                    return;
                }

                using (process)
                {
                    // Read all output from PowerShell
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Split the output into lines
                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    // Expecting exactly four lines (one per hash algorithm)
                    if (lines.Length < 4)
                    {
                        hashStatus = "Incomplete hash data";
                        return;
                    }

                    // Store each computed hash in lowercase for comparison purposes
                    md5Hash = lines[0].Trim().ToLowerInvariant();
                    sha1Hash = lines[1].Trim().ToLowerInvariant();
                    sha256Hash = lines[2].Trim().ToLowerInvariant();
                    sha512Hash = lines[3].Trim().ToLowerInvariant();

                    var expected = LoadReferenceHashes(); // Loads from testfile\TestHashes.txt

                    string statusMD5 = (!expected.TryGetValue("MD5", out var expectedMD5) || string.IsNullOrWhiteSpace(expectedMD5))
                        ? "unknown"
                        : (md5Hash.Equals(expectedMD5, StringComparison.OrdinalIgnoreCase) ? "OK" : "Missing");

                    string statusSHA1 = (!expected.TryGetValue("SHA1", out var expectedSHA1) || string.IsNullOrWhiteSpace(expectedSHA1))
                        ? "unknown"
                        : (sha1Hash.Equals(expectedSHA1, StringComparison.OrdinalIgnoreCase) ? "OK" : "Missing");

                    string statusSHA256 = (!expected.TryGetValue("SHA256", out var expectedSHA256) || string.IsNullOrWhiteSpace(expectedSHA256))
                        ? "unknown"
                        : (sha256Hash.Equals(expectedSHA256, StringComparison.OrdinalIgnoreCase) ? "OK" : "Missing");

                    string statusSHA512 = (!expected.TryGetValue("SHA512", out var expectedSHA512) || string.IsNullOrWhiteSpace(expectedSHA512))
                        ? "unknown"
                        : (sha512Hash.Equals(expectedSHA512, StringComparison.OrdinalIgnoreCase) ? "OK" : "Missing");

                    // Output the computed hashes to the debug console
                    Debug.WriteLine($"MD5:      {md5Hash} ({statusMD5})");
                    Debug.WriteLine($"SHA1:     {sha1Hash} ({statusSHA1})");
                    Debug.WriteLine($"SHA256:   {sha256Hash} ({statusSHA256})");
                    Debug.WriteLine($"SHA512:   {sha512Hash} ({statusSHA512})");


                    // Set status flag based on whether all hashes were successfully computed
                    hashStatus = (string.IsNullOrEmpty(md5Hash) ||
                                 string.IsNullOrEmpty(sha1Hash) ||
                                 string.IsNullOrEmpty(sha256Hash) ||
                                 string.IsNullOrEmpty(sha512Hash))
                                 ? "Hash missing"
                                 : "OK";
                }
            }
            catch (Exception)
            {
                hashStatus = "Error";
            }
        }

        // Update status indicators
        private void UpdateStatusIndicators()
        {
            var expected = LoadReferenceHashes(); // Loads from testfile\TestHashes.txt

            string statusMD5 = (!expected.TryGetValue("MD5", out var expectedMD5) || string.IsNullOrWhiteSpace(expectedMD5))
                ? "unknown"
                : (md5Hash.Equals(expectedMD5, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA1 = (!expected.TryGetValue("SHA1", out var expectedSHA1) || string.IsNullOrWhiteSpace(expectedSHA1))
                ? "unknown"
                : (sha1Hash.Equals(expectedSHA1, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA256 = (!expected.TryGetValue("SHA256", out var expectedSHA256) || string.IsNullOrWhiteSpace(expectedSHA256))
                ? "unknown"
                : (sha256Hash.Equals(expectedSHA256, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA512 = (!expected.TryGetValue("SHA512", out var expectedSHA512) || string.IsNullOrWhiteSpace(expectedSHA512))
                ? "unknown"
                : (sha512Hash.Equals(expectedSHA512, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            UpdateStatusColour(indicatorMD5, statusMD5);
            UpdateStatusColour(indicatorSHA1, statusSHA1);
            UpdateStatusColour(indicatorSHA256, statusSHA256);
            UpdateStatusColour(indicatorSHA512, statusSHA512);
        }

        // Change indicator colour based on status
        private void UpdateStatusColour(System.Windows.Shapes.Ellipse indicator, string status)
        {
            Brush brush = status.ToLower() switch
            {
                "green" => Brushes.Green,
                "red" => Brushes.Red,
                "unknown" => Brushes.Yellow,
                _ => Brushes.Gray
            };

            indicator.Fill = brush;
        }

    }

}
