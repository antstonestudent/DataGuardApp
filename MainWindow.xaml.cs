using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using DataGuardApp.hashing;

namespace DataGuardApp
{
    public partial class MainWindow : Window
    {
        private string selectedFile = string.Empty;
        private bool columnsVisible = false; // Controls collapsible panel visibility

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Polygon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LeftText.Visibility = columnsVisible ? Visibility.Collapsed : Visibility.Visible;
            RightText.Visibility = columnsVisible ? Visibility.Collapsed : Visibility.Visible;
            columnsVisible = !columnsVisible;
        }

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

        private void OfficialHashTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (OfficialHashTextBox.Text == "Enter Official Hash")
            {
                OfficialHashTextBox.Text = "";
            }
        }

        private void ComputeHashButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFile))
            {
                MessageBox.Show("Please select a file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(OfficialHashTextBox.Text))
            {
                MessageBox.Show("Please enter the official hash.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string officialHash = OfficialHashTextBox.Text.Trim().ToLower();

            string md5Hash = MD5Hasher.ComputeHash(selectedFile);
            string sha1Hash = SHA1Hasher.ComputeHash(selectedFile);
            string sha256Hash = SHA256Hasher.ComputeHash(selectedFile);
            string sha512Hash = SHA512Hasher.ComputeHash(selectedFile);

            MD5ResultText.Text = $"MD5: {md5Hash} - " + (md5Hash == officialHash ? "✅ Match" : "❌ No Match");
            SHA1ResultText.Text = $"SHA-1: {sha1Hash} - " + (sha1Hash == officialHash ? "✅ Match" : "❌ No Match");
            SHA256ResultText.Text = $"SHA-256: {sha256Hash} - " + (sha256Hash == officialHash ? "✅ Match" : "❌ No Match");
            SHA512ResultText.Text = $"SHA-512: {sha512Hash} - " + (sha512Hash == officialHash ? "✅ Match" : "❌ No Match");
        }
    }
}
