using Elegance.Bindings;
using Elegance.Components;
using Elegance.Components.Search;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace Elegance.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {

        private List<DocumentItemBinding> documentItemCollection;
        private ICommand closeTabCommand;
        public ICommand CloseTabCommand
        {
            get
            {
                return closeTabCommand ??
                    (closeTabCommand = new SimpleCommand
                    { CanExecuteDelegate = x => false });
            }
        }
        private int untitled_counter;
        private TreeViewItem lastPopulatedTreeView;
        private FindReplaceDialog findReplaceDialog;
        private string xmlPath;

        private string defaultDirectoryPath;
        private int defaultTheme;
        public MainWindow()
        {
            InitializeComponent();
            documentItemCollection = new List<DocumentItemBinding>();
            untitled_counter = 1;
            findReplaceDialog = new FindReplaceDialog(this);

            xmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Elegance", "Settings.xml");
            if (!Directory.Exists(Path.GetDirectoryName(xmlPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));
            }

            defaultDirectoryPath = "SKIP";
            defaultTheme = 1;
        }

        public MainWindow(string[] args) : this()
        { 
            if (args.Length > 0)
            {
                foreach (string fileName in args)
                {
                    AddTabItem(fileName);
                }
            }
        }

        public void saveXml()
        {
            using (XmlWriter writer = XmlWriter.Create(xmlPath))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Elegance");

                writer.WriteStartElement("Theme");
                writer.WriteString(defaultTheme.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Path");
                if (String.IsNullOrEmpty(defaultDirectoryPath))
                {
                    writer.WriteString("SKIP");
                }
                else
                {
                    writer.WriteString(defaultDirectoryPath);
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

        }
        
        public void loadXml()
        {
            if (File.Exists(xmlPath))
            {
                using (XmlReader reader = XmlReader.Create(xmlPath))
                {
                    bool theme = false;
                    bool path = false;
                    while (reader.Read())
                    {
                        if (reader.Name.Equals("Theme") && !theme)
                        {
                            reader.Read();
                            defaultTheme = 1;
                            if (!Int32.TryParse(reader.Value.Trim(), out defaultTheme))
                            {
                                defaultTheme = 1;
                            }
                            theme = true;
                        }
                        else if (reader.Name.Equals("Path") && !path)
                        {
                            reader.Read();
                            defaultDirectoryPath = reader.Value.Trim();

                            path = true;
                        }
                    }
                }
            }
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            loadXml();

            switch (defaultTheme)
            {
                case 0:
                    ThemeManager.ChangeAppTheme(Application.Current, "BaseLight");
                    lightThemeMenuItem.IsChecked = true;
                    darkThemeMenuItem.IsChecked = false;
                    break;
                case 1:
                    ThemeManager.ChangeAppTheme(Application.Current, "BaseDark");
                    lightThemeMenuItem.IsChecked = false;
                    darkThemeMenuItem.IsChecked = true;
                    break;
            }

            if (!Directory.Exists(defaultDirectoryPath) || defaultDirectoryPath.Equals("SKIP"))
            {
                defaultDirectoryPath = "SKIP";
                populateTreeView(Directory.GetCurrentDirectory());
            }
            else
            {
                populateTreeView(defaultDirectoryPath);
            }
        }

        private void populateTreeView(string path)
        {
            treeView.Items.Clear();
            TreeViewItem treeViewItem = new TreeViewItem() { Header = Path.GetFileName(path), Tag = path };
            addDirectoryToTree(treeViewItem, path);
            lastPopulatedTreeView = treeViewItem;
            treeView.Items.Add(treeViewItem);
        }

        private void addDirectoryToTree(TreeViewItem parentItem, string root)
        {
            string[] directoryList = Directory.GetDirectories(root);
            foreach (string directory in directoryList)
            {
                TreeViewItem item = new TreeViewItem()
                {
                    Header = Path.GetFileName(directory),
                    Tag = directory
                };
                addDirectoryToTree(item, item.Tag.ToString());
                parentItem.Items.Add(item);
            }

            string[] fileList = Directory.GetFiles(root);
            foreach (string file in fileList)
            {
                parentItem.Items.Add(new TreeViewItem()
                {
                    Header = Path.GetFileName(file),
                    Tag = file
                });
            }
        }

        private void AddTabItem(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            foreach (DocumentItemBinding di in documentItemCollection)
            {
                if (di.Path.CompareTo(path) == 0)
                {
                    SetSelectedTabItem(di.TabItem);
                    return;
                }
            }

            string tabName = Path.GetFileName(path);
            DocumentItemBinding documentItemBinding = new DocumentItemBinding()
            {
                Path = path,
                TextEdit = new TextEditor()
                {
                    Foreground = getAvalonForeground(),
                    ShowLineNumbers = true,
                    LineNumbersForeground = new SolidColorBrush(Colors.DeepSkyBlue),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 0),
                    SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path)),
                    Text = File.ReadAllText(path),
                    Tag = path,
                },
                TabItem = new MetroTabItem()
                {
                    Header = tabName,
                    Tag = path,
                    CloseButtonEnabled = true
                }
            };

            if (documentItemBinding.TextEdit.SyntaxHighlighting == null)
            {
                documentItemBinding.TextEdit.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            }

            SearchPanel.Install(documentItemBinding.TextEdit.TextArea);
            languageComboBox.SelectedItem = documentItemBinding.TextEdit.SyntaxHighlighting;

            documentItemBinding.TextEdit.TextChanged += TextEdit_TextChanged;

            documentItemBinding.TextEdit.TextArea.Caret.PositionChanged += new EventHandler(avalonEditCaretPositionChanged);
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandProperty, new Binding("CloseTabCommand"));
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandParameterProperty, new Binding()
            {
                RelativeSource = RelativeSource.Self,
                Path = new PropertyPath("Header")
            });

            Grid grid = new Grid() { Background = new SolidColorBrush(Color.FromRgb(229, 229, 229)) };
            grid.Children.Add(documentItemBinding.TextEdit);
            documentItemBinding.TabItem.Content = grid;
            tabControl.Items.Add(documentItemBinding.TabItem);
            SetSelectedTabItem(documentItemBinding.TabItem);

            documentItemCollection.Add(documentItemBinding);
        }

        private void TextEdit_TextChanged(object sender, EventArgs e)
        {
            try {
                TabItem tabItem = GetSelectedDocumentItem().TabItem;
                if (tabItem == null)
                {
                    return;
                }
                if (!tabItem.Header.ToString().Contains("*"))
                {
                    tabItem.Header += "*";
                }
            }
            catch { }
        }

        private bool SaveChanges()
        {
            TabItem tabItem = GetSelectedDocumentItem().TabItem;
            if (tabItem == null)
            {
                return false;
            }
            if (tabItem.Header.ToString().Contains("*"))
            {
                switch (MessageBox.Show(this, "Do you want to save changes?", "Elegance", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.None))
                {
                    case MessageBoxResult.Yes:
                        saveMenuItem_Click(this, null);
                        return true;
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            return true;
        }

        private Brush getAvalonForeground()
        {
            if (darkThemeMenuItem.IsChecked)
            {
                return new SolidColorBrush(Colors.White);
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        private void AddUntitledTabItem()
        {
            string tabName = "untitled";
            if (untitled_counter != 1)
            {
                tabName += untitled_counter;
            }
            untitled_counter++;


            DocumentItemBinding documentItemBinding = new DocumentItemBinding()
            {
                Path = "ELEGANCE_NULL_PATH",
                TextEdit = new TextEditor()
                {
                    Foreground = getAvalonForeground(),
                    ShowLineNumbers = true,
                    LineNumbersForeground = new SolidColorBrush(Colors.DeepSkyBlue),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 0),
                    SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#"),
                    Tag = "ELEGANCE_NULL_PATH",
                },
                TabItem = new MetroTabItem()
                {
                    Header = tabName,
                    Tag = "ELEGANCE_NULL_PATH",
                    CloseButtonEnabled = true,
                }
            };
            SearchPanel.Install(documentItemBinding.TextEdit.TextArea);

            documentItemBinding.TextEdit.TextChanged += TextEdit_TextChanged;

            documentItemBinding.TextEdit.TextArea.Caret.PositionChanged += new EventHandler(avalonEditCaretPositionChanged);
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandProperty, new Binding("CloseTabCommand"));
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandParameterProperty, new Binding()
            {
                RelativeSource = RelativeSource.Self,
                Path = new PropertyPath("Header")
            });

            Grid grid = new Grid() { Background = new SolidColorBrush(Color.FromRgb(229, 229, 229)) };
            grid.Children.Add(documentItemBinding.TextEdit);
            documentItemBinding.TabItem.Content = grid;
            tabControl.Items.Add(documentItemBinding.TabItem);
            SetSelectedTabItem(documentItemBinding.TabItem);

            documentItemCollection.Add(documentItemBinding);
        }

        private void avalonEditCaretPositionChanged(object sender, EventArgs e)
        {
            TextEditor textEditor = GetSelectedTextEditor();
            if (textEditor == null)
            {
                return;
            }
            caretPositionStatusBarItem.Content = "Line " + textEditor.TextArea.Caret.Line + ", Column " + textEditor.TextArea.Caret.VisualColumn;
        }

        private void SetSelectedTabItem(MetroTabItem tabItem)
        {
            tabControl.SelectedItem = tabItem;
        }

        public TextEditor GetSelectedTextEditor()
        {
            foreach (DocumentItemBinding di in documentItemCollection)
            {
                if (di.TabItem == tabControl.SelectedItem)
                {
                    return di.TextEdit;
                }
            }
            return null;
        }

        private DocumentItemBinding GetSelectedDocumentItem()
        {
            foreach (DocumentItemBinding di in documentItemCollection)
            {
                if (di.TabItem == tabControl.SelectedItem)
                {
                    return di;
                }
            }
            return null;
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem == null)
            {
                return;
            }
            fullPathStatusBarItem.Content = (treeView.SelectedItem as TreeViewItem).Tag;

            if (File.GetAttributes(fullPathStatusBarItem.Content.ToString()).HasFlag(FileAttributes.Directory))
            {
                treeView.ContextMenu = treeView.Resources["DirectoryTreeViewContext"] as ContextMenu;
            }
            else
            {
                treeView.ContextMenu = treeView.Resources["FileTreeViewContext"] as ContextMenu;
            }
        }

        private void languageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try {
                GetSelectedTextEditor().SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(languageComboBox.SelectedItem.ToString());
            }
            catch{  }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                languageComboBox.SelectedItem = GetSelectedTextEditor().SyntaxHighlighting;
            }
            catch { }
        }

        private void newFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddUntitledTabItem();
        }

        private void openFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All Files (*.*)|*.*"};
            if (openFileDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
                {
                    AddTabItem(openFileDialog.FileName);
                }
            }

        }

        private void openDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog())
            {
                if (!string.IsNullOrEmpty(openFolderDialog.FileName) && Directory.Exists(openFolderDialog.FileName)) 
                {
                    populateTreeView(openFolderDialog.FileName);
                    defaultDirectoryPath = openFolderDialog.FileName;
                }
            } 
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DocumentItemBinding di = GetSelectedDocumentItem();
            if (di == null)
            {
                return;
            }
            if (File.Exists(di.TextEdit.Tag.ToString()) && di.TabItem.Header.ToString().Contains("*"))
            {
                File.WriteAllText(di.TextEdit.Tag.ToString(), di.TextEdit.Text);
                di.TabItem.Header = Path.GetFileName(di.TabItem.Tag.ToString());
            }
            else if (di.TextEdit.Tag.ToString().CompareTo("ELEGANCE_NULL_PATH") == 0)
            {
                saveAsMenuItem_Click(this, e);
            }
        }

        private void saveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DocumentItemBinding di = GetSelectedDocumentItem();
            if (di == null)
            {
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "Saving " + di.TabItem.Header,
                Filter = "All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                di.ChangePath(saveFileDialog.FileName);
                File.WriteAllText(saveFileDialog.FileName, di.TextEdit.Text);
                populateTreeView(lastPopulatedTreeView.Tag.ToString());
            }
        }

        private async void saveAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            List<string> errorFiles = new List<String>();
            foreach (DocumentItemBinding di in documentItemCollection)
            {
                if (File.Exists(di.TextEdit.Tag.ToString()))
                {
                    File.WriteAllText(di.TextEdit.Tag.ToString(), di.TextEdit.Text);
                }
                else if (di.TextEdit.Tag.ToString().CompareTo("ELEGANCE_NULL_PATH") == 0)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog()
                    {
                        Title = "Saving " + di.TabItem.Header,
                        Filter = "All Files (*.*)|*.*"
                    };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        di.ChangePath(saveFileDialog.FileName);
                        File.WriteAllText(saveFileDialog.FileName, di.TextEdit.Text);
                    }
                    else
                    {
                        errorFiles.Add(di.TabItem.Header.ToString());
                        continue;
                    }
                }
                else
                {
                    errorFiles.Add(di.TabItem.Header.ToString());
                    continue;
                }
            }
            if (errorFiles.Count != 0)
            {
                await this.ShowMessageAsync("Elegance - Save All", "The following files were not saved: " + String.Join(", ", errorFiles.ToArray()));
            }
            else
            {
                await this.ShowMessageAsync("Elegance - Save All", "All files were successfully saved.");
            }
            populateTreeView(lastPopulatedTreeView.Tag.ToString());
        }

        private void newWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
        }

        private void closeWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!File.GetAttributes(fullPathStatusBarItem.Content.ToString()).HasFlag(FileAttributes.Directory))
                {
                    AddTabItem(fullPathStatusBarItem.Content.ToString());
                }
            }
            catch { }
        }

        private void tabControl_TabItemClosingEvent(object sender, BaseMetroTabControl.TabItemClosingEventArgs e)
        {
            if (e.ClosingTabItem.Tag.ToString() == "ELEGANCE_ABOUT")
            {
                return;
            }
            if (SaveChanges())
            {
                foreach (DocumentItemBinding di in documentItemCollection)
                {
                    if (di.TabItem == e.ClosingTabItem)
                    {
                        documentItemCollection.Remove(di);
                        break;
                    }
                } 
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DocumentItemBinding documentItemBinding = new DocumentItemBinding()
            {
                Path = "ELEGANCE_ABOUT",
                TextEdit = new TextEditor()
                {
                    Foreground = getAvalonForeground(),
                    ShowLineNumbers = true,
                    LineNumbersForeground = new SolidColorBrush(Colors.DeepSkyBlue),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 0),
                    SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#"),
                    Tag = "ELEGANCE_ABOUT",
                    Text = "Application: Elegance" + Environment.NewLine + "Version: 1.0.0.1" + Environment.NewLine + "Creator: Xenon" + Environment.NewLine + "Project: https://kisenon.github.io/projects/Elegance/"
                },
                TabItem = new MetroTabItem()
                {
                    Header = "About Elegance",
                    Tag = "ELEGANCE_ABOUT",
                    CloseButtonEnabled = true,
                }
            };
            documentItemBinding.TextEdit.TextArea.Caret.PositionChanged += new EventHandler(avalonEditCaretPositionChanged);
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandProperty, new Binding("CloseTabCommand"));
            BindingOperations.SetBinding(documentItemBinding.TabItem, MetroTabItem.CloseTabCommandParameterProperty, new Binding()
            {
                RelativeSource = RelativeSource.Self,
                Path = new PropertyPath("Header")
            });

            Grid grid = new Grid() { Background = new SolidColorBrush(Color.FromRgb(229, 229, 229)) };
            grid.Children.Add(documentItemBinding.TextEdit);
            documentItemBinding.TabItem.Content = grid;
            tabControl.Items.Add(documentItemBinding.TabItem);
            SetSelectedTabItem(documentItemBinding.TabItem);
        }

        private void newCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            newFileMenuItem_Click(this, e);
        }

        private void openCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            openFileMenuItem_Click(this, e);
        }

        private void saveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            saveMenuItem_Click(this, e);
        }

        private void saveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            saveAsMenuItem_Click(this, e);
        }

        private async void renameFileTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            string renamedFile = await this.ShowInputAsync("Elegance - Rename a File", "What would you like to rename " + Path.GetFileName(fullPathStatusBarItem.Content.ToString()) + " to?");
            if (renamedFile == null)
            {
                return;
            }
            File.Move(fullPathStatusBarItem.Content.ToString(), Path.Combine(Path.GetDirectoryName(fullPathStatusBarItem.Content.ToString()), renamedFile));
            populateTreeView(lastPopulatedTreeView.Tag.ToString());
        }

        private async void deleteFileTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            if (await this.ShowMessageAsync("Elegance - Delete a File", "Are you sure you would like to delete " + Path.GetFileName(fullPathStatusBarItem.Content.ToString()) + "?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", ColorScheme = MetroDialogColorScheme.Theme}) == MessageDialogResult.Affirmative)
            {
                File.Delete(fullPathStatusBarItem.Content.ToString());
                populateTreeView(lastPopulatedTreeView.Tag.ToString());
            }
        }

        private void openContainingFolderTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + fullPathStatusBarItem.Content);
        }

        private void deleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (treeView.ContextMenu == treeView.Resources["FileTreeViewContext"] as ContextMenu)
            {
                deleteFileTreeViewContext_Click(this, e);
            }
        }

        private async void newFileTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            string newFile = await this.ShowInputAsync("Elegance - Create a New File", "What would you like to name your file?");
            if (newFile == null)
            {
                return;
            }
            File.Create(Path.Combine(fullPathStatusBarItem.Content.ToString(), newFile)).Close();
            AddTabItem(Path.Combine(fullPathStatusBarItem.Content.ToString(), newFile));
            populateTreeView(lastPopulatedTreeView.Tag.ToString());
        }

        private async void renameFolderTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            string renamedFolder = await this.ShowInputAsync("Elegance - Rename a Folder", "What would you like to rename " + Path.GetFileName(fullPathStatusBarItem.Content.ToString()) + " to?");
            if (renamedFolder == null)
            {
                return;
            }
            Directory.Move(fullPathStatusBarItem.Content.ToString(), Path.Combine(Path.GetDirectoryName(fullPathStatusBarItem.Content.ToString()), renamedFolder));
            populateTreeView(lastPopulatedTreeView.Tag.ToString());
        }

        private async void newFolderTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            string createdFolder = await this.ShowInputAsync("Elegance - Create a Folder", "What would you like to name your folder?");
            if (createdFolder == null)
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine(fullPathStatusBarItem.Content.ToString(), createdFolder));
            populateTreeView(lastPopulatedTreeView.Tag.ToString());
        }

        private async void deleteFolderTreeViewContext_Click(object sender, RoutedEventArgs e)
        {
            if (await this.ShowMessageAsync("Elegance - Delete a Folder", "Are you sure you would like to delete " + Path.GetFileName(fullPathStatusBarItem.Content.ToString()) + " and all the containing files and folders?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", ColorScheme = MetroDialogColorScheme.Theme }) == MessageDialogResult.Affirmative)
            {
                Directory.Delete(fullPathStatusBarItem.Content.ToString(), true);
                populateTreeView(lastPopulatedTreeView.Tag.ToString());
            }
        }

        private void clearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = GetSelectedTextEditor();
            if (textEditor == null)
            {
                return;
            }
            textEditor.Clear();
        }

        private void replaceCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!findReplaceDialog.IsOpened)
            {
                findReplaceDialog.Show();
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveXml();
            findReplaceDialog.OverrideClosing = true;
            findReplaceDialog.Close();
        }

        private async void gotoLineCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                TextEditor editor = GetSelectedTextEditor();
                int line = Int32.Parse(await this.ShowInputAsync("Elegance - Go to Line", "Line Number (1 - " + editor.LineCount + "):"));
                editor.Focus();
                editor.TextArea.Caret.Line = line;         
            }
            catch { }
        }

        public static readonly DependencyProperty ToggleFullScreenProperty =
            DependencyProperty.Register("ToggleFullScreen",
                                        typeof(bool),
                                        typeof(MainWindow),
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

        public void AvalonChangeThemes()
        {
            foreach (DocumentItemBinding di in documentItemCollection)
            {
                di.TextEdit.Foreground = getAvalonForeground();
            }
        }

        private void fullScreenMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen = true;
        }

        private void fullScreenMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen = false;
        }

        private void darkThemeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            lightThemeMenuItem.IsChecked = false;
            darkThemeMenuItem.IsChecked = true;
            defaultTheme = 1;
            ThemeManager.ChangeAppTheme(Application.Current, "BaseDark");
            AvalonChangeThemes();
        }

        private void darkThemeMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (lightThemeMenuItem.IsChecked == false)
            {
                darkThemeMenuItem.IsChecked = true;
            }
        }

        private void lightThemeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            lightThemeMenuItem.IsChecked = true;
            darkThemeMenuItem.IsChecked = false;
            defaultTheme = 0;
            ThemeManager.ChangeAppTheme(Application.Current, "BaseLight");
            AvalonChangeThemes();
        }

        private void lightThemeMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (darkThemeMenuItem.IsChecked == false)
            {
                lightThemeMenuItem.IsChecked = true;
            }
        }

        private void tabControl_Drop(object sender, DragEventArgs e)
        {
            String[] fileNames = (String[])e.Data.GetData(DataFormats.FileDrop, true);
            if (fileNames.Length > 0)
            {
                foreach (string fileName in fileNames)
                {
                    AddTabItem(fileName);
                }

            }
            e.Handled = true;
        }

        private void juxtaposeEditorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new JuxtaposeWindow().ShowDialog();
        }
    }
}