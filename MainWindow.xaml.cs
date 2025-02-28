using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using DataGuardApp.hashing;

namespace DataGuardApp
{
    public partial class MainWindow : Window
    {
        private string selectedFile = string.Empty;
        private string[] storedHashes;
        private string hashFilePath = "stored_hashes.txt"; // Path to hash file

        public MainWindow()
        {
            InitializeComponent();
            LoadStoredHashes();
        }

        private void LoadStoredHashes()
        {
            if (File.Exists(hashFilePath))
            {
                storedHashes = File.ReadAllLines(hashFilePath);
            }
            else
            {
                storedHashes = new string[0];
                File.WriteAllText(hashFilePath, ""); // This function will create an empty file if it doesn't exist
            }
        }

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

            // Function that updates UI indicators (traffic lights)
            UpdateTrafficLight(indicator1, GetHashStatus(md5Hash));    // MD5
            UpdateTrafficLight(indicator2, GetHashStatus(sha1Hash));   // SHA-1
            UpdateTrafficLight(indicator3, GetHashStatus(sha256Hash)); // SHA-256
            UpdateTrafficLight(indicator4, GetHashStatus(sha512Hash)); // SHA-512

            // Function to store hashes for future reference
            SaveHash(md5Hash);
            SaveHash(sha1Hash);
            SaveHash(sha256Hash);
            SaveHash(sha512Hash);
        }

        private string GetHashStatus(string computedHash)
        {
            string storedHash = FindStoredHash(computedHash);

            if (storedHash == null)
            {
                return "Yellow"; // No stored hash found
            }
            else if (computedHash == storedHash)
            {
                return "Green"; // Hash matches
            }
            else
            {
                return "Red"; // Hash mismatch
            }
        }

        private void UpdateTrafficLight(System.Windows.Shapes.Ellipse light, string status)
        {
            switch (status)
            {
                case "Green":
                    light.Fill = Brushes.Green;
                    break;
                case "Yellow":
                    light.Fill = Brushes.Yellow;
                    break;
                case "Red":
                    light.Fill = Brushes.Red;
                    break;
            }
        }

        private string FindStoredHash(string computedHash)
        {
            return storedHashes.FirstOrDefault(hash => hash.Equals(computedHash, StringComparison.OrdinalIgnoreCase));
        }

        private void SaveHash(string newHash)
        {
            if (!storedHashes.Contains(newHash)) // This function is used to avoid duplicated entries
            {
                File.AppendAllText(hashFilePath, newHash + Environment.NewLine);
                storedHashes = File.ReadAllLines(hashFilePath); // This function is to refresh stored hashes
            }
        }
    }
}
