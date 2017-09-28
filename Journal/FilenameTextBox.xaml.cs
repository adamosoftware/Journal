using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdamOneilSoftware
{
    public enum FilenameTextBoxMode
    {
        Open, Save
    }

    public partial class FilenameTextBox : UserControl
    {
        public FilenameTextBox()
        {
            InitializeComponent();
            Mode = FilenameTextBoxMode.Open;
        }

        public string DefaultExt { get; set; }
        public FilenameTextBoxMode Mode { get; set; }
        public string Filter { get; set; }

        public string Text
        {
            get { return tbFilename.Text; }
            set { tbFilename.Text = value; }
        }

        private void btnFilename_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void SelectFile()
        {
            switch (Mode)
            {
                case FilenameTextBoxMode.Open:
                    OpenFileDialog openDlg = new OpenFileDialog();
                    openDlg.Filter = Filter;
                    if (openDlg.ShowDialog().Value)
                    {
                        tbFilename.Text = openDlg.FileName;
                    }
                    break;

                case FilenameTextBoxMode.Save:
                    SaveFileDialog saveDlg = new SaveFileDialog();
                    saveDlg.Filter = Filter;
                    saveDlg.DefaultExt = DefaultExt;
                    if (saveDlg.ShowDialog().Value)
                    {
                        tbFilename.Text = saveDlg.FileName;
                    }
                    break;
            }
        }

        private void tbFilename_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectFile();
        }
    }
}