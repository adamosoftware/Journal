using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
