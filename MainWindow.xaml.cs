using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using Windows.Media.Devices;

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

        [GeneratedRegex("ReferrerUrl=(.*)", RegexOptions.Compiled)]
        private static partial Regex MyRegex1();

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

                // Wait 5 seconds then reset progress bar
                await Task.Delay(5000);
                await SlowlyResetProgressBarAsync();
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

                    // Wait 5 seconds then reset progress bar
                    await Task.Delay(5000);
                    await SlowlyResetProgressBarAsync();
                }
            }
        }

        private void ResetProgressBar()
        {
            progressBar.Value = 0;
            topStop.Offset = 0;
            bottomStop.Offset = 0;
        }

        private void UpdateProgressBar(double percent)
        {
            progressBar.Value = percent;
            double normalized = percent / 100.0;
            topStop.Offset = normalized;
            bottomStop.Offset = normalized;
        }

        private async Task SlowlyResetProgressBarAsync()
        {
            while (progressBar.Value > 0)
            {
                double newValue = progressBar.Value - 1;
                UpdateProgressBar(newValue);
                await Task.Delay(50);
            }
        }

        public class FileChecksumInfo
        {
            public string FileName { get; set; } = string.Empty;
            public string MD5 { get; set; } = string.Empty;
            public string SHA1 { get; set; } = string.Empty;
            public string SHA256 { get; set; } = string.Empty;
            public string SHA512 { get; set; } = string.Empty;
            public DateTime? LastProcessed { get; set; } = null;
            public DateTime? LastModified { get; set; } = null;
        }

        private static void WriteChecksumsToCsv(string csvPath, List<FileChecksumInfo> records)
        {
            using var writer = new StreamWriter(csvPath);
            writer.WriteLine("FileName,MD5,SHA1,SHA256,SHA512,LastProcessed,LastModified");

            foreach (var rec in records)
            {
                string LastProcessed = rec.LastProcessed.HasValue ? rec.LastProcessed.Value.ToString("o") : "";
                string LastModified = rec.LastModified.HasValue ? rec.LastModified.Value.ToString("o") : "";
                writer.WriteLine($"{rec.FileName},{rec.MD5},{rec.SHA1},{rec.SHA256},{rec.SHA512},{LastProcessed},{LastModified}");
            }
        }

        private static void UpdateChecksumsCsv(string filePath, string md5, string sha1, string sha256, string sha512, DateTime lastProcessed, DateTime lastModified)
        {
            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "checksums.csv");
            List<FileChecksumInfo> records = [];

            if (File.Exists(csvPath))
            {
                records = LoadReferenceHashesFromCsv();
            }

            string fileName = Path.GetFileName(filePath);

            var record = records.FirstOrDefault(r =>
                r.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
                r.MD5.Equals(md5, StringComparison.OrdinalIgnoreCase) &&
                r.SHA1.Equals(sha1, StringComparison.OrdinalIgnoreCase) &&
                r.SHA256.Equals(sha256, StringComparison.OrdinalIgnoreCase) &&
                r.SHA512.Equals(sha512, StringComparison.OrdinalIgnoreCase));

            if (record != null)
            {
                record.LastProcessed = lastProcessed;
                record.LastModified = lastModified;
            }
            else
            {
                record = new FileChecksumInfo
                {
                    FileName = fileName,
                    MD5 = md5,
                    SHA1 = sha1,
                    SHA256 = sha256,
                    SHA512 = sha512,
                    LastProcessed = lastProcessed,
                    LastModified = lastModified
                };
                records.Add(record);
            }

            using var writer = new StreamWriter(csvPath);
            writer.WriteLine("FileName,MD5,SHA1,SHA256,SHA512,LastProcessed,LastModified");
            foreach (var rec in records)
            {
                string lp = rec.LastProcessed.HasValue ? rec.LastProcessed.Value.ToString("o") : "";
                string lm = rec.LastModified.HasValue ? rec.LastModified.Value.ToString("o") : "";
                writer.WriteLine($"{rec.FileName},{rec.MD5},{rec.SHA1},{rec.SHA256},{rec.SHA512},{lp},{lm}");
            }

        }

        // Loads the csv document to compare the computed hashes against (batch files in testfile folder, or a few selected windows iso files)
        private static List<FileChecksumInfo> LoadReferenceHashesFromCsv()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "checksums.csv");
            var fileChecksumList = new List<FileChecksumInfo>();

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Reference file not found at: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return fileChecksumList;
            }

            // Read all lines from the CSV
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.StartsWith("FileName", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Split the line by comma
                var parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    var info = new FileChecksumInfo
                    {
                        FileName = parts[0].Trim(),
                        MD5 = parts[1].Trim().ToLowerInvariant(),
                        SHA1 = parts[2].Trim().ToLowerInvariant(),
                        SHA256 = parts[3].Trim().ToLowerInvariant(),
                        SHA512 = parts[4].Trim().ToLowerInvariant(),
                    };
                    // check if there is a column for LastProcessed
                    if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]))
                    {
                        if (DateTime.TryParse(parts[5].Trim(), out DateTime lastProcessed))
                        {
                            info.LastProcessed = lastProcessed;
                        }
                    }
                    // check if there is a column for LastModified
                    if (parts.Length >= 7 && !string.IsNullOrWhiteSpace(parts[6]))
                    {
                        if (DateTime.TryParse(parts[6].Trim(), out DateTime lastModified))
                        {
                            info.LastModified = lastModified;
                        }
                    }

                    fileChecksumList.Add(info);
                }
            }
            return fileChecksumList;
        }
        
        // Discover hashes of selectedFile using background thread in chunks
        private async Task ComputeHashesSelectedFile(string selectedFile, IProgress<double> progress, int bufferSize = 65536) // 64KB
        {
            using FileStream stream = File.OpenRead(selectedFile);
            long totalBytes = stream.Length;
            long processedBytes = 0;

            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            using var sha256 = SHA256.Create();
            using var sha512 = SHA512.Create();
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
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

            // If a reference entry is found, compare each hash, otherwise mark as untrusted
            string statusMD5 = (reference != null && !string.IsNullOrWhiteSpace(reference.MD5) &&
                                md5Hash.Equals(reference.MD5, StringComparison.OrdinalIgnoreCase))
                                ? "match" : "untrusted";

            string statusSHA1 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA1) &&
                                sha1Hash.Equals(reference.SHA1, StringComparison.OrdinalIgnoreCase))
                                ? "match" : "untrusted";

            string statusSHA256 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA256) &&
                                sha256Hash.Equals(reference.SHA256, StringComparison.OrdinalIgnoreCase))
                                ? "match" : "untrusted";

            string statusSHA512 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA512) &&
                                sha512Hash.Equals(reference.SHA512, StringComparison.OrdinalIgnoreCase))
                                ? "match" : "untrusted";

            UpdateStatusColour(indicatorMD5, statusMD5);
            UpdateStatusColour(indicatorSHA1, statusSHA1);
            UpdateStatusColour(indicatorSHA256, statusSHA256);
            UpdateStatusColour(indicatorSHA512, statusSHA512);
        }

        // Change indicator colours based on status
        private static void UpdateStatusColour(System.Windows.Shapes.Ellipse indicator, string status)
        {
            Brush brush = status.ToLower() switch
            {
                "match" => Brushes.Chartreuse,
                "untrusted" => Brushes.Red,
                "processing" => Brushes.DarkOrange,
                _ => Brushes.DarkGray
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
                statusMD5 = statusSHA1 = statusSHA256 = statusSHA512 = "untrusted";
            }
            else
            {
                statusMD5 = (reference != null && !string.IsNullOrWhiteSpace(reference.MD5) &&
                                md5Hash.Equals(reference.MD5, StringComparison.OrdinalIgnoreCase))
                                ? "match" : "untrusted";

                statusSHA1 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA1) &&
                                    sha1Hash.Equals(reference.SHA1, StringComparison.OrdinalIgnoreCase))
                                    ? "match" : "untrusted";

                statusSHA256 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA256) &&
                                    sha256Hash.Equals(reference.SHA256, StringComparison.OrdinalIgnoreCase))
                                    ? "match" : "untrusted";

                statusSHA512 = (reference != null && !string.IsNullOrWhiteSpace(reference.SHA512) &&
                                    sha512Hash.Equals(reference.SHA512, StringComparison.OrdinalIgnoreCase))
                                    ? "match" : "untrusted";
            }            

            StringBuilder sb = new();            
            string currentTime = DateTime.Now.ToString("g"); // To include current date and time

            // Output string for the text output window            
            sb.AppendLine($"File: {Path.GetFileName(selectedFile)}");
            sb.AppendLine($"MD5:     {statusMD5}");
            sb.AppendLine($"SHA1:    {statusSHA1}");
            sb.AppendLine($"SHA256:  {statusSHA256}");
            sb.AppendLine($"SHA512:  {statusSHA512}");
            sb.AppendLine($"Tested On: {currentTime}");
            sb.AppendLine(new string('-', 18));

            // Append to the output TextBox and scroll to the end
            PrependOutput(sb.ToString());
        }

        // Event handler to show the most recent file tested in the textbox up the top
        private void PrependOutput(string text)
        {
            // Create a new chunk of text with the output text
            Paragraph newParagraph = new(new Run(text));

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

        private void GetFileSource_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedFile) || !File.Exists(selectedFile))
            {
                MessageBox.Show("No file selected or file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string filePath = selectedFile;
            var directory = Path.GetDirectoryName(selectedFile);
            if (string.IsNullOrEmpty(directory))
            {
                MessageBox.Show("Could not determine file directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string source = GetFileSource(selectedFile);
            string origin = GetFileOrigin(selectedFile);
            string versionInfo = CheckForNewerVersion(selectedFile, directory);

            string message = $"File: {Path.GetFileName(selectedFile)}\n" +
                            $"Source: {source}\n" +
                            $"{origin}\n" +
                            $"{versionInfo}";
            MessageBox.Show(message, "File Information", MessageBoxButton.OK, MessageBoxImage.Information);

            // Update the CSV record with computed hash values and dates
            string md5 = md5Hash;
            string sha1 = sha1Hash;
            string sha256 = sha256Hash;
            string sha512 = sha512Hash;

            FileInfo fi = new(filePath);
            DateTime lastModified = fi.LastWriteTime;
            DateTime lastProcessed = DateTime.Now;

            UpdateChecksumsCsv(filePath, md5, sha1, sha256, sha512, lastProcessed, lastModified);
        }

        private static string GetFileSource(string filePath)
        {
            string zonePath = filePath + ":Zone.Identifier";
            if (!File.Exists(filePath))
                return "File does not exist.";

            if (File.Exists(zonePath))
            {
                foreach (string line in File.ReadAllLines(zonePath))
                {
                    if (line.Contains("ZoneId=3"))
                        return "Untrusted (Internet Download)";
                    if (line.Contains("ZoneId=2"))
                        return "Trusted (Local Intranet)";
                }
            }
            return "Local File (Not Downloaded)";
        }             

        static string GetFileOrigin(string filePath, Regex? myRegex1)
        {
            string zonePath = filePath + ":Zone.Identifier";
            if (!File.Exists(filePath))
                return "File does not exist.";
            if (File.Exists(zonePath))
            {
                string content = File.ReadAllText(zonePath);
                if (myRegex1 != null && myRegex1.Match(content).Success == true)
                    return "File Origin: " + myRegex1.Match(content).Groups[1].Value;
            }
            return "Origin Unknown";
        }
        private static string GetFileOrigin(string filePath)
        {
            return GetFileOrigin(filePath, MyRegex1());
        }

        static IEnumerable<string> GetFilesSafe(string path, string searchPattern)
        {
            Queue<string> dirs = new();
            dirs.Enqueue(path);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Dequeue();
                string[] subDirs = [];
                string[] files = [];

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }

                try
                {
                    files = Directory.GetFiles(currentDir, searchPattern);
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }

                foreach (string file in files)
                {
                    yield return file;
                }

                foreach (string subDir in subDirs)
                {
                    dirs.Enqueue(subDir);
                }
            }
        }

        static string CheckForNewerVersion(string filePath, string directory)
        {
            if (!File.Exists(filePath) || !Directory.Exists(directory))
            {
                return "Invalid file or directory.";
            }

            FileInfo originalFile = new(filePath);
            DateTime latestModification = originalFile.LastWriteTime;
            string latestFile = filePath;

            foreach (string file in GetFilesSafe(directory, originalFile.Name))
            {
                FileInfo fileInfo = new(file);
                if (fileInfo.LastWriteTime > latestModification)
                {
                    latestModification = fileInfo.LastWriteTime;
                    latestFile = file;
                }
            }

            return latestFile != filePath ? $"Newer version: {latestFile}" : "No newer version found.";
        }

        private void WebsiteSource_Click(object sender, RoutedEventArgs e)
        {
            string origin = GetFileOrigin(selectedFile);

            string prefix = "File Origin:";
            string originUrl = "";
            if (!string.IsNullOrWhiteSpace(origin) && origin.StartsWith(prefix))
            {
                originUrl = origin[prefix.Length..].Trim();
            }

            if (string.IsNullOrWhiteSpace(originUrl))
            {
                MessageBox.Show("No valid source URL found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string message = $"Do you want to continue to {originUrl}?";

            MessageBoxResult result = MessageBox.Show(message, "Open Website", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(originUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open website: " + ex.Message);
                }
            }
        }

        // Place new code above here
    }

}
