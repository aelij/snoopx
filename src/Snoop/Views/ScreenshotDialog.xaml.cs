// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Snoop.Utilities;

namespace Snoop.Views
{
    /// <summary>
    /// Interaction logic for ScreenShotDialog.xaml
    /// </summary>
    public partial class ScreenshotDialog
    {
        public static readonly RoutedCommand PrintCommand = new RoutedCommand("Print", typeof(ScreenshotDialog));
        public static readonly RoutedCommand SaveCommand = new RoutedCommand("Save", typeof(ScreenshotDialog));
        public static readonly RoutedCommand CancelCommand = new RoutedCommand("Cancel", typeof(ScreenshotDialog));

        public ScreenshotDialog()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(PrintCommand, HandlePrint, HandleCanSave));
            CommandBindings.Add(new CommandBinding(SaveCommand, HandleSave, HandleCanSave));
            CommandBindings.Add(new CommandBinding(CancelCommand, HandleCancel, (x, y) => y.CanExecute = true));
        }

        private void HandlePrint(object sender, ExecutedRoutedEventArgs e)
        {
            var visual = DataContext as Visual;
            if (visual == null) return;

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(visual, "SnoopScreenshot");

                Close();
            }
        }

        #region FilePath Dependency Property
        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register
            (
                "FilePath",
                typeof(string),
                typeof(ScreenshotDialog),
                new UIPropertyMetadata(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\SnoopScreenshot.png")
            );

        #endregion

        private void HandleCanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!(DataContext is Visual))
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }
        private void HandleSave(object sender, ExecutedRoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "png",
                FileName = FilePath
            };
            if (fileDialog.ShowDialog(this).Value)
            {
                FilePath = fileDialog.FileName;
                ((Visual)DataContext).SaveVisual(int.Parse(
                        ((TextBlock)((ComboBoxItem)DpiBox.SelectedItem).Content).Text), FilePath);

                Close();
            }
        }

        private void HandleCancel(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
