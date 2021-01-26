using Elegance.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;

namespace Elegance.Components.Search
{
    /// <summary>
    /// Interaction logic for FindReplaceDialog.xaml
    /// </summary>
    public partial class FindReplaceDialog : MetroWindow
    {
        public bool IsOpened;
        public bool OverrideClosing;
        private MainWindow parentWindow;
        public FindReplaceDialog(MainWindow parentWnd)
        {
            InitializeComponent();
            OverrideClosing = false;
            IsOpened = false;
            parentWindow = parentWnd;
        }


        private void metroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            IsOpened = true;
        }

        private void metroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!OverrideClosing)
            {
                e.Cancel = true;
                IsOpened = false;
                Hide();
            }
        }

        private void metroWindow_Closed(object sender, System.EventArgs e)
        {
            IsOpened = false;
        }

        private void findNextButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!FindNext(findTextBox.Text))
                SystemSounds.Beep.Play();
        }

        private void replaceButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Regex regex = GetRegex(findTextBox.Text);
            TextEditor editor = parentWindow.GetSelectedTextEditor();
            string input = editor.Text.Substring(editor.SelectionStart, editor.SelectionLength);
            Match match = regex.Match(input);
            bool replaced = false;
            if (match.Success && match.Index == 0 && match.Length == input.Length)
            {
                editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, replaceTextBox.Text);
                replaced = true;
            }


            if (!FindNext(findTextBox.Text) && !replaced)
                SystemSounds.Beep.Play();
        }

        private void replaceAllButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to replace all occurences of \"" + findTextBox.Text + "\" with \"" + replaceTextBox.Text + "\"?", "Elegance - Replace All", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                TextEditor editor = parentWindow.GetSelectedTextEditor();
                Regex regex = GetRegex(findTextBox.Text, true);
                int offset = 0;
                int count = 0;
                editor.BeginChange();
                foreach (Match match in regex.Matches(editor.Text))
                {
                    editor.Document.Replace(offset + match.Index, match.Length, replaceTextBox.Text);
                    offset += replaceTextBox.Text.Length - match.Length;
                    count++;
                }
                editor.EndChange();

                MessageBox.Show(count + " occurences(s) replaced.", "Elegance - Replace All", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Regex GetRegex(string textToFind, bool leftToRight = false)
        {
            RegexOptions options = RegexOptions.None;
            if (searchUpCheckBox.IsChecked == true && !leftToRight)
                options |= RegexOptions.RightToLeft;
            if (matchCaseCheckBox.IsChecked == false)
                options |= RegexOptions.IgnoreCase;

            if (useRegexCheckBox.IsChecked == true)
            {
                return new Regex(textToFind, options);
            }
            else
            {
                string pattern = Regex.Escape(textToFind);
                if (wildCardsCheckBox.IsChecked == true)
                    pattern = pattern.Replace("\\*", ".*").Replace("\\?", ".");
                if (matchWholeWordCheckBox.IsChecked == true)
                    pattern = "\\b" + pattern + "\\b";
                return new Regex(pattern, options);
            }
        }

        private bool FindNext(string textToFind)
        {
            Regex regex = GetRegex(textToFind);
            TextEditor editor = parentWindow.GetSelectedTextEditor();
            int start = regex.Options.HasFlag(RegexOptions.RightToLeft) ? editor.SelectionStart : editor.SelectionStart + editor.SelectionLength;
            Match match = regex.Match(editor.Text, start);

            if (!match.Success)
            {
                if (regex.Options.HasFlag(RegexOptions.RightToLeft))
                    match = regex.Match(editor.Text, editor.Text.Length);
                else
                    match = regex.Match(editor.Text, 0);
            }

            if (match.Success)
            {
                editor.Select(match.Index, match.Length);
                TextLocation loc = editor.Document.GetLocation(match.Index);
                editor.ScrollTo(loc.Line, loc.Column);
            }

            return match.Success;
        }
    }
}
