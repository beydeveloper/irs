using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Irsafe
{
    public partial class AssemblyEditorWindow : Window
    {
        public AssemblyInjectionService.AssemblyPayload AssemblyPayload { get; private set; }
        public bool DialogResult { get; private set; } = false;

        public AssemblyEditorWindow()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AssemblyCodeTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter assembly code", "Create Payload", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AssemblyPayload = CreatePayloadFromUI();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private AssemblyInjectionService.AssemblyPayload CreatePayloadFromUI()
        {
            var payload = new AssemblyInjectionService.AssemblyPayload
            {
                AssemblyCode = AssemblyCodeTextBox.Text,
                ExecutionType = (ExecutionTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "bat",
                Description = DescriptionTextBox.Text,
                AutoExecute = AutoExecuteCheckBox.IsChecked ?? false,
                CreatedDate = DateTime.Now
            };

            // Add metadata
            payload.Metadata["Creator"] = "Ýrsafe Assembly Editor";
            payload.Metadata["Version"] = "1.0";

            return payload;
        }
    }
}