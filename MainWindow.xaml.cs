using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DataGuardApp
{
    public partial class MainWindow : Window
    {
        private string selectedFile = string.Empty; // Function that stores the path of the selected file
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
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
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

        private async void FileButton_Click(object sender, RoutedEventArgs e) // To open file explorer and select file
        {
            NoSelectedFile();

            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".bat", // Default extension
                Filter = "All Files (*.*)|*.*" // Filter can be adjusted
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                selectedFile = openFileDialog.FileName;
                ResetProgressBar();

                var progressReporter = new Progress<double>(percent =>
                {
                    UpdateProgressBar(percent);
                });

                // Process the file while reporting progress
                await ComputeHashesSelectedFile(selectedFile, progressReporter);

                // Progress bar is set to 100% after the processing completes
                UpdateProgressBar(100);

                // Update Indicators and Output window
                UpdateStatusIndicators();
                UpdateOutputWindow();
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
        private async void FileButton_Drop(object sender, DragEventArgs e)
        {
            NoSelectedFile();

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
                    ResetProgressBar();

                    var progressReporter = new Progress<double>(percent =>
                    {
                        UpdateProgressBar(percent);
                    });

                    await ComputeHashesSelectedFile(selectedFile, progressReporter);

                    UpdateProgressBar(100);
                    
                    UpdateStatusIndicators();
                    UpdateOutputWindow();
                }
            }
        }

        private void NoSelectedFile()
        {
            if (string.IsNullOrWhiteSpace(selectedFile) || !File.Exists(selectedFile))
            {
                UpdateProgressBar(100);
                return;
            }
        }

        public class FileChecksumInfo
        {
            public string FileName { get; set; } = string.Empty;
            public string MD5 { get; set; } = string.Empty;
            public string SHA1 { get; set; } = string.Empty;
            public string SHA256 { get; set; } = string.Empty;
            public string SHA512 { get; set; } = string.Empty;
        }

        // Loads the csv document to compare the computed hashes against (batch files in testfile folder, or a few selected windows iso files)
        private List<FileChecksumInfo> LoadReferenceHashesFromCsv()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "checksums.csv");
            var fileChecksumList = new List<FileChecksumInfo>();

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Reference file not fount at: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return fileChecksumList;
            }

            // Read all lines from the CSV
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                // Skip header row if present
                if (line.StartsWith("FileName", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Split the line by comma
                var parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    fileChecksumList.Add(new FileChecksumInfo
                    {
                        FileName = parts[0].Trim(),
                        MD5 = parts[1].Trim().ToLowerInvariant(),
                        SHA1 = parts[2].Trim().ToLowerInvariant(),
                        SHA256 = parts[3].Trim().ToLowerInvariant(),
                        SHA512 = parts[4].Trim().ToLowerInvariant(),
                    });
                }
            }
            return fileChecksumList;
        }
        
        // Discover hashes of selectedFile using background thread in chunks
        private async Task ComputeHashesSelectedFile(string selectedFile, IProgress<double> progress, int bufferSize = 65536) // 64KB
        {
            NoSelectedFile();

            using FileStream stream = File.OpenRead(selectedFile);
            long totalBytes = stream.Length;
            long processedBytes = 0;

            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            using var sha256 = SHA256.Create();
            using var sha512 = SHA512.Create();
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            
            while ((bytesRead = await stream.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                // Processing of the current chunk in each algorithm
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                UpdateStatusColour(indicatorMD5, "processing");
                sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                UpdateStatusColour(indicatorSHA1, "processing");
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                UpdateStatusColour(indicatorSHA256, "processing");
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
                UpdateStatusColour(indicatorSHA512, "processing");

                processedBytes += bytesRead;
                // Report progress as a percentage
                progress.Report((double)processedBytes / totalBytes * 100);
            }      
                        
            // Finalize hash computation
            md5.TransformFinalBlock(buffer, 0, 0);            
            sha1.TransformFinalBlock(buffer, 0, 0);            
            sha256.TransformFinalBlock(buffer, 0, 0);            
            sha512.TransformFinalBlock(buffer, 0, 0);            

            // Convert the computed hash byte arrays to strings
            md5Hash = Convert.ToHexStringLower(md5.Hash ?? []);
            sha1Hash = Convert.ToHexStringLower(sha1.Hash ?? []);
            sha256Hash = Convert.ToHexStringLower(sha256.Hash ?? []);
            sha512Hash = Convert.ToHexStringLower(sha512.Hash ?? []);            
        }
        
        // Update status indicators
        private void UpdateStatusIndicators()
        {
            var fileList = LoadReferenceHashesFromCsv(); // Loads from resources\checksums.csv into a list

            // Search for an entry matching the selected file name
            var reference = fileList.Find(info =>
                info.FileName.Equals(Path.GetFileName(selectedFile), StringComparison.OrdinalIgnoreCase));

            // If a reference entry is found, compare each hash, otherwise mark as unknown
            string statusMD5 = reference == null || string.IsNullOrWhiteSpace(reference.MD5)
                ? "unknown"
                : (md5Hash.Equals(reference.MD5, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA1 = reference == null || string.IsNullOrWhiteSpace(reference.SHA1)
                ? "unknown"
                : (sha1Hash.Equals(reference.SHA1, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA256 = reference == null || string.IsNullOrWhiteSpace(reference.SHA256)
                ? "unknown"
                : (sha256Hash.Equals(reference.SHA256, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            string statusSHA512 = reference == null || string.IsNullOrWhiteSpace(reference.SHA512)
                ? "unknown"
                : (sha512Hash.Equals(reference.SHA512, StringComparison.OrdinalIgnoreCase) ? "green" : "red");

            UpdateStatusColour(indicatorMD5, statusMD5);
            UpdateStatusColour(indicatorSHA1, statusSHA1);
            UpdateStatusColour(indicatorSHA256, statusSHA256);
            UpdateStatusColour(indicatorSHA512, statusSHA512);
        }

        // Change rivet indicator colour based on status
        private void UpdateStatusColour(System.Windows.Shapes.Ellipse indicator, string status)
        {
            Brush brush = status.ToLower() switch
            {
                "green" => Brushes.Chartreuse,
                "red" => Brushes.Red,
                "processing" => Brushes.Yellow,
                "unknown" => Brushes.Orange,
                _ => Brushes.Gray
            };

            indicator.Fill = brush;
        }

        // Visual output for text output window
        private void UpdateOutputWindow()
        {
            var fileList = LoadReferenceHashesFromCsv();

            var reference = fileList.FirstOrDefault(info =>
                info.FileName.Equals(Path.GetFileName(selectedFile), StringComparison.OrdinalIgnoreCase));

            string statusMD5, statusSHA1, statusSHA256, statusSHA512;

            if (reference == null)
            {
                statusMD5 = statusSHA1 = statusSHA256 = statusSHA512 = "unknown";
            }
            else
            {
                statusMD5 = string.IsNullOrWhiteSpace(reference.MD5)
                    ? "unknown"
                    : (md5Hash.Equals(reference.MD5, StringComparison.OrdinalIgnoreCase) ? "Matched" : "No hash found");

                statusSHA1 = string.IsNullOrWhiteSpace(reference.SHA1)
                    ? "unknown"
                    : (sha1Hash.Equals(reference.SHA1, StringComparison.OrdinalIgnoreCase) ? "Matched" : "No hash found");

                statusSHA256 = string.IsNullOrWhiteSpace(reference.SHA256)
                    ? "unknown"
                    : (sha256Hash.Equals(reference.SHA256, StringComparison.OrdinalIgnoreCase) ? "Matched" : "No hash found");

                statusSHA512 = string.IsNullOrWhiteSpace(reference.SHA512)
                    ? "unknown"
                    : (sha512Hash.Equals(reference.SHA512, StringComparison.OrdinalIgnoreCase) ? "Matched" : "No hash found");
            }            

            StringBuilder sb = new StringBuilder();            
            string currentTime = DateTime.Now.ToString("g"); // To include current date and time

            // Output string for the text output window            
            sb.AppendLine($"File:   {Path.GetFileName(selectedFile)}");
            sb.AppendLine($"MD5:    {statusMD5}");
            sb.AppendLine($"SHA1:   {statusSHA1}");
            sb.AppendLine($"SHA256: {statusSHA256}");
            sb.AppendLine($"SHA512: {statusSHA512}");
            sb.AppendLine($"Tested On: {currentTime}");
            sb.AppendLine(new string('-', 18));

            // Append to the output TextBox and scroll to the end
            PrependOutput(sb.ToString());
        }

        // Event handler to show the most recent file tested in the textbox up the top
        private void PrependOutput(string text)
        {
            // Create a new chunk of text with the output text
            Paragraph newParagraph = new Paragraph(new Run(text));

            // If there is already content in the text box, insert before the first block; otherwise, add it
            if (outputTextBox.Document.Blocks.FirstBlock != null)
            {
                outputTextBox.Document.Blocks.InsertBefore(outputTextBox.Document.Blocks.FirstBlock, newParagraph);
            }
            else
            {
                outputTextBox.Document.Blocks.Add(newParagraph);
            }
        }

        // Event handler for a method to convert progress percentage into a gradient offset
        private void UpdateProgressBar(double progressPercentage)
        {
            double progress = Math.Max(0, Math.Min(190, progressPercentage)) / 100.0;
            progressStop.Offset = progress;
        }

        // Event handler to reset the progress bar for each new file
        private void ResetProgressBar()
        {
            progressStop.Offset = 0;
        }

    }

}
