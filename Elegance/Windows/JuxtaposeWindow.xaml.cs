using Elegance.Components.Search;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Elegance.Windows
{
    /// <summary>
    /// Interaction logic for JuxtaposeWindow.xaml
    /// </summary>
    public partial class JuxtaposeWindow : MetroWindow
    {
        public JuxtaposeWindow()
        {
            InitializeComponent();

            SearchPanel.Install(leftTextEditor.TextArea);
            leftTextEditor.TextArea.Caret.PositionChanged += new EventHandler(avalonEditLeftCaretPositionChanged);
            SearchPanel.Install(rightTextEditor.TextArea);
            rightTextEditor.TextArea.Caret.PositionChanged += new EventHandler(avalonEditRightCaretPositionChanged);
            
        }


        private void avalonEditLeftCaretPositionChanged(object sender, EventArgs e)
        {
            caretLeftStatusBarItem.Content = "Line " + leftTextEditor.TextArea.Caret.Line + ", Column " + leftTextEditor.TextArea.Caret.VisualColumn;
        }

        private void avalonEditRightCaretPositionChanged(object sender, EventArgs e)
        {
            caretRightStatusBarItem.Content = "Line " + rightTextEditor.TextArea.Caret.Line + ", Column " + rightTextEditor.TextArea.Caret.VisualColumn;
        }

        private void juxtaposeWindow_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            leftTabControl.Margin = new Thickness(0, 20, Width / 2, 35);
            rightTabControl.Margin = new Thickness(Width / 2, 20, 0, 35);
            leftStatusBar.Margin = new Thickness(0, 0, Width / 2, 0);
            rightStatusBar.Margin = new Thickness(Width / 2, 0, 0, 0);

        }

        private void languageLeftComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                leftTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(languageLeftComboBox.SelectedItem.ToString());
            }
            catch { }
        }

        private void languageRightComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                rightTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(languageRightComboBox.SelectedItem.ToString());
            }
            catch { }
        }

        private async Task<bool> leftSaveChanges()
        {
            if (leftTabItem.Header.ToString().Contains("*"))
            {
                switch (await this.ShowMessageAsync("Elegance", "Do you want to save changes?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", FirstAuxiliaryButtonText = "Cancel", ColorScheme = MetroDialogColorScheme.Theme }))
                {
                    case MessageDialogResult.Affirmative:
                        saveMenuLeftItem_Click(this, null);
                        return true;
                    case MessageDialogResult.Negative:
                        return true;
                    case MessageDialogResult.FirstAuxiliary:
                        return false;
                }
            }
            return true;
        }

        private async Task<bool> rightSaveChanges()
        {
            if (rightTabItem.Header.ToString().Contains("*"))
            {
                switch (await this.ShowMessageAsync("Elegance", "Do you want to save changes?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", FirstAuxiliaryButtonText = "Cancel", ColorScheme = MetroDialogColorScheme.Theme }))
                {
                    case MessageDialogResult.Affirmative:
                        saveMenuRightItem_Click(this, null);
                        return true;
                    case MessageDialogResult.Negative:
                        return true;
                    case MessageDialogResult.FirstAuxiliary:
                        return false;
                }
            }
            return true;
        }

        private async void newFileLeftMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (await leftSaveChanges())
            {
                leftTextEditor.Text = "";
                leftTabItem.Header = "Untitled";
            }
        }

        private async void newFileRightMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (await rightSaveChanges())
            {
                rightTextEditor.Text = "";
                rightTabItem.Header = "Untitled";
            }
        }

        
        private async void openCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        { 
            int focus = 3;
            if (leftTextEditor.IsFocused || leftTextEditor.TextArea.IsFocused)
            {
                focus = 1;
                if (!await leftSaveChanges())
                {
                    return;
                }
            }
            else if (rightTextEditor.IsFocused || rightTextEditor.TextArea.IsFocused)
            {
                focus = 2;
                if (!await rightSaveChanges())
                {
                    return;
                }
            }
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All Files (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
                {
                    switch (focus)
                    {
                        case 1:
                            AddToLeft(openFileDialog.FileName);
                            break;
                        case 2:
                            AddToRight(openFileDialog.FileName);
                            break;
                        case 3:
                            if (leftTabItem.Header.Equals("Untitled"))
                            {
                                AddToLeft(openFileDialog.FileName);
                            }
                            else if (rightTabItem.Header.Equals("Untitled"))
                            {
                                AddToRight(openFileDialog.FileName);
                            }
                            break;
                    }
                }
            }
        }

        private void AddToLeft(string fileName)
        {
            leftTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
            leftTextEditor.Text = File.ReadAllText(fileName);
            leftTextEditor.Tag = fileName;
            leftTabItem.Header = Path.GetFileName(fileName);
            leftTabItem.Tag = fileName;

            if (leftTextEditor.SyntaxHighlighting == null)
            {
                leftTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            }

            
            languageLeftComboBox.SelectedItem = leftTextEditor.SyntaxHighlighting;
            fullPathLeftStatusBarItem.Content = fileName;

        }

        private void AddToRight(string fileName)
        {
            rightTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
            rightTextEditor.Text = File.ReadAllText(fileName);
            rightTextEditor.Tag = fileName;
            rightTabItem.Header = Path.GetFileName(fileName);
            rightTabItem.Tag = fileName;

            if (rightTextEditor.SyntaxHighlighting == null)
            {
                rightTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            }


            languageRightComboBox.SelectedItem = rightTextEditor.SyntaxHighlighting;
            fullPathRightStatusBarItem.Content = fileName;
        }

        private void leftTextEditor_TextChanged(object sender, EventArgs e)
        {
            if (!leftTabItem.Header.ToString().Contains("*"))
            {
                leftTabItem.Header += "*";
            }
        }

        private void rightTextEditor_TextChanged(object sender, EventArgs e)
        {
            if (!rightTabItem.Header.ToString().Contains("*"))
            {
                rightTabItem.Header += "*";
            }
        }

        private void saveCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (leftTabItem.Header.Equals("Untitled*") || rightTabItem.Header.ToString().Equals("Untitled*")
                    || leftTabItem.Header.Equals("Untitled") || rightTabItem.Header.ToString().Equals("Untitled"))
            {
                saveAsCommandBinding_Executed(this, e);
                return;
            }

            int focus = 3;
            if (leftTextEditor.IsFocused || leftTextEditor.TextArea.IsFocused)
            {
                focus = 1;
            }
            else if (rightTextEditor.IsFocused || rightTextEditor.TextArea.IsFocused)
            {
                focus = 2;
            }

            switch (focus)
            {
                case 1:
                    if (File.Exists(leftTextEditor.Tag.ToString()) && leftTabItem.Header.ToString().Contains("*"))
                    {
                        File.WriteAllText(leftTextEditor.Tag.ToString(), leftTextEditor.Text);
                        leftTabItem.Header = Path.GetFileName(leftTextEditor.Tag.ToString());
                    }
                    break;
                case 2:
                    if (File.Exists(rightTextEditor.Tag.ToString()) && rightTabItem.Header.ToString().Contains("*"))
                    {
                        File.WriteAllText(rightTextEditor.Tag.ToString(), rightTextEditor.Text);
                        rightTabItem.Header = Path.GetFileName(rightTextEditor.Tag.ToString());
                    }
                    break;
            }
        }

        private void saveAsCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            string relativeName = "";
            int focus = 3;
            if (leftTextEditor.IsFocused || leftTextEditor.TextArea.IsFocused)
            {
                focus = 1;
                relativeName = leftTabItem.Header.ToString().Replace("*", "");
            }
            else if (rightTextEditor.IsFocused || rightTextEditor.TextArea.IsFocused)
            {
                focus = 2;
                relativeName = rightTabItem.Header.ToString().Replace("*", "");
            }
            else
            {
                return;
            }


            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "Saving " + relativeName,
                Filter = "All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    switch (focus)
                    {
                        case 1:
                            if (leftTabItem.Header.ToString().Contains("*"))
                            {
                                File.WriteAllText(saveFileDialog.FileName, leftTextEditor.Text);
                                AddToLeft(saveFileDialog.FileName);
                            }
                            break;
                        case 2:
                            if (rightTabItem.Header.ToString().Contains("*"))
                            {
                                File.WriteAllText(saveFileDialog.FileName, rightTextEditor.Text);
                                AddToRight(saveFileDialog.FileName);
                            }
                            break;
                    }
                }
            }
        }

        private async void openFileLeftMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!await leftSaveChanges())
            {
                return;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All Files (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
                {
                    AddToLeft(openFileDialog.FileName);
                }
            }
        }

        private async void openFileRightMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!await leftSaveChanges())
            {
                return;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All Files (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
                {
                    AddToRight(openFileDialog.FileName);
                }
            }
        }

        private void saveMenuLeftItem_Click(object sender, RoutedEventArgs e)
        {
            if (leftTabItem.Header.Equals("Untitled*") || leftTabItem.Header.Equals("Untitled"))
            {
                saveAsMenuLeftItem_Click(this, e);
            }
            else
            {
                if (File.Exists(leftTextEditor.Tag.ToString()) && leftTabItem.Header.ToString().Contains("*"))
                {
                    File.WriteAllText(leftTextEditor.Tag.ToString(), leftTextEditor.Text);
                    leftTabItem.Header = Path.GetFileName(leftTextEditor.Tag.ToString());
                }
            }
        }

        private void saveMenuRightItem_Click(object sender, RoutedEventArgs e)
        {
            if (rightTabItem.Header.ToString().Equals("Untitled*") ||
                rightTabItem.Header.ToString().Equals("Untitled"))
            {
                saveAsMenuRightItem_Click(this, e);
            }
            else
            {
                if (File.Exists(rightTextEditor.Tag.ToString()) && rightTabItem.Header.ToString().Contains("*"))
                {
                    File.WriteAllText(rightTextEditor.Tag.ToString(), leftTextEditor.Text);
                    rightTabItem.Header = Path.GetFileName(rightTextEditor.Tag.ToString());
                }
            }
        }

        private void saveAsMenuLeftItem_Click(object sender, RoutedEventArgs e)
        {
            string relativeName = leftTabItem.Header.ToString().Replace("*", "");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "Saving " + relativeName,
                Filter = "All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    if (leftTabItem.Header.ToString().Contains("*"))
                    {
                        File.WriteAllText(saveFileDialog.FileName, leftTextEditor.Text);
                        AddToLeft(saveFileDialog.FileName);
                    }
                }
            }
        }

        private void saveAsMenuRightItem_Click(object sender, RoutedEventArgs e)
        {
            string relativeName = rightTabItem.Header.ToString().Replace("*", "");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "Saving " + relativeName,
                Filter = "All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    if (rightTabItem.Header.ToString().Contains("*"))
                    {
                        File.WriteAllText(saveFileDialog.FileName, rightTextEditor.Text);
                        AddToRight(saveFileDialog.FileName);
                    }
                }
            }
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private async void gotoLineCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                TextEditor editor = null;
                if (leftTextEditor.IsFocused || leftTextEditor.TextArea.IsFocused)
                {
                    editor = leftTextEditor;
                }
                else if (rightTextEditor.IsFocused || rightTextEditor.TextArea.IsFocused)
                {
                    editor = rightTextEditor;
                }
                else
                {
                    return;
                }
                int line = Int32.Parse(await this.ShowInputAsync("Elegance - Go to Line", "Line Number (1 - " + editor.LineCount + "):"));
                editor.Focus();
                editor.TextArea.Caret.Line = line;
            }
            catch { }
        }

        public static readonly DependencyProperty ToggleFullScreenProperty =
            DependencyProperty.Register("ToggleFullScreen",
                                        typeof(bool),
                                        typeof(JuxtaposeWindow),
                                        new PropertyMetadata(default(bool), ToggleFullScreenPropertyChangedCallback));

        private static void ToggleFullScreenPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            MetroWindow metroWindow = (MetroWindow)dependencyObject;
            if (e.OldValue != e.NewValue)
            {
                if ((bool)e.NewValue)
                {
                    metroWindow.UseNoneWindowStyle = true;
                    metroWindow.IgnoreTaskbarOnMaximize = true;
                    metroWindow.WindowState = WindowState.Maximized;
                }
                else
                {
                    metroWindow.UseNoneWindowStyle = false;
                    metroWindow.ShowTitleBar = true;
                    metroWindow.IgnoreTaskbarOnMaximize = false;
                }
            }
        }

        public bool ToggleFullScreen
        {
            get { return (bool)GetValue(ToggleFullScreenProperty); }
            set { SetValue(ToggleFullScreenProperty, value); }
        }

        private void fullScreenMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen = true;
        }

        private void fullScreenMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen = false;
        }

        private async void leftTabControl_Drop(object sender, DragEventArgs e)
        {
            if (!await leftSaveChanges())
            {
                return;
            }

            String[] fileNames = (String[])e.Data.GetData(DataFormats.FileDrop, true);
            if (fileNames.Length > 0)
            {
                if (!string.IsNullOrEmpty(fileNames[0]) && File.Exists(fileNames[0]))
                {
                    AddToLeft(fileNames[0]);
                }
            }
            e.Handled = true;
        }

        private async void rightTabControl_Drop(object sender, DragEventArgs e)
        {
            if (!await rightSaveChanges())
            {
                return;
            }
            String[] fileNames = (String[])e.Data.GetData(DataFormats.FileDrop, true);
            if (fileNames.Length > 0)
            {
                if (!string.IsNullOrEmpty(fileNames[0]) && File.Exists(fileNames[0]))
                {
                    AddToRight(fileNames[0]);
                }
            }
            e.Handled = true;
        }
    }
}
