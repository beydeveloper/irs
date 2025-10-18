using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Irsafe
{
    public partial class MainWindow : Window
    {
        private readonly WpfSteganographyService _steganographyService;
        
        public MainWindow()
        {
            InitializeComponent();
            _steganographyService = new WpfSteganographyService();
            
            UpdateStatus("Ýrsafe - Ready for text steganography");
        }
        
        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Image File",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|PNG Files|*.png|JPEG Files|*.jpg;*.jpeg|BMP Files|*.bmp|GIF Files|*.gif|All Files|*.*",
                FilterIndex = 1
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (IOFile.Exists(openFileDialog.FileName))
                    {
                        ImagePathTextBox.Text = openFileDialog.FileName;
                        var fileInfo = new FileInfo(openFileDialog.FileName);
                        UpdateStatus($"Image selected: {IOPath.GetFileName(openFileDialog.FileName)} ({fileInfo.Length / 1024:N0} KB)");
                    }
                    else
                    {
                        UpdateStatus("Selected file does not exist");
                        MessageBox.Show("The selected file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error accessing file: {ex.Message}");
                    MessageBox.Show($"Error accessing the selected file:\n{ex.Message}", "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void LoadTextButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Text File",
                Filter = "Text Files|*.txt|All Files|*.*",
                FilterIndex = 1
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string content = IOFile.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                    TextInputBox.Text = content;
                    UpdateStatus($"Text file loaded: {IOPath.GetFileName(openFileDialog.FileName)} ({content.Length} characters)");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error loading text file: {ex.Message}");
                    MessageBox.Show($"Error loading text file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void SaveTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextInputBox.Text))
            {
                UpdateStatus("No text to save");
                MessageBox.Show("No text to save.", "No Text", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Text File",
                Filter = "Text Files|*.txt|All Files|*.*",
                FilterIndex = 1,
                DefaultExt = "txt"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    IOFile.WriteAllText(saveFileDialog.FileName, TextInputBox.Text, Encoding.UTF8);
                    UpdateStatus($"Text file saved: {IOPath.GetFileName(saveFileDialog.FileName)}");
                    MessageBox.Show($"Text saved successfully!\nFile: {IOPath.GetFileName(saveFileDialog.FileName)}", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error saving text file: {ex.Message}");
                    MessageBox.Show($"Error saving text file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void HideTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ImagePathTextBox.Text))
            {
                UpdateStatus("Please select an image file first");
                MessageBox.Show("Please select an image file first.", "No Image Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(TextInputBox.Text))
            {
                UpdateStatus("Please enter text to hide");
                MessageBox.Show("Please enter text to hide.", "No Text Entered", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("Hiding text in image...");
                
                byte[] resultImageBytes = _steganographyService.HideTextInImage(ImagePathTextBox.Text, TextInputBox.Text);
                
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Image with Hidden Text",
                    Filter = "PNG Files|*.png|JPEG Files|*.jpg|BMP Files|*.bmp|All Files|*.*",
                    FilterIndex = 1,
                    DefaultExt = "png",
                    FileName = IOPath.GetFileNameWithoutExtension(ImagePathTextBox.Text) + "_with_hidden_text"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    IOFile.WriteAllBytes(saveFileDialog.FileName, resultImageBytes);
                    UpdateStatus($"Text successfully hidden: {IOPath.GetFileName(saveFileDialog.FileName)}");
                    MessageBox.Show($"Text successfully hidden in image!\n\nSaved as: {IOPath.GetFileName(saveFileDialog.FileName)}\nHidden text length: {TextInputBox.Text.Length} characters", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateStatus("Save operation cancelled");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error hiding text: {ex.Message}");
                MessageBox.Show($"Error hiding text in image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExtractTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ImagePathTextBox.Text))
            {
                UpdateStatus("Please select an image file first");
                MessageBox.Show("Please select an image file first.", "No Image Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("Extracting text from image...");
                
                string extractedText = _steganographyService.ExtractTextFromImage(ImagePathTextBox.Text);
                
                if (string.IsNullOrEmpty(extractedText))
                {
                    UpdateStatus("No hidden text found in image");
                    MessageBox.Show("No hidden text found in the selected image.", "No Text Found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    TextInputBox.Text = extractedText;
                    UpdateStatus($"Text extracted successfully ({extractedText.Length} characters)");
                    MessageBox.Show($"Text extracted successfully!\n\nFound: {extractedText.Length} characters\nText has been loaded into the text area.", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error extracting text: {ex.Message}");
                MessageBox.Show($"Error extracting text from image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
    }
}