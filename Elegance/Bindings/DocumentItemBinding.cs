using ICSharpCode.AvalonEdit;
using MahApps.Metro.Controls;

namespace Elegance.Bindings
{
    class DocumentItemBinding
    {
        public string Path { get; set; }
        public TextEditor TextEdit { get; set; }
        public MetroTabItem TabItem { get; set; }

        public DocumentItemBinding()
        {

        }

        public void ChangePath(string path)
        {
            Path = path;
            TextEdit.Tag = path;
            TabItem.Tag = path;
            TabItem.Header = System.IO.Path.GetFileName(path);
        }
    }
}
