using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
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
using System.Windows.Shapes;

namespace AdamOneilSoftware
{
	public partial class MainWindow3 : Window
	{
		private bool _modified = false;
		private int _entryID = 0;
		private JournalDb _db = null;

		public MainWindow3()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_modified) _db.SaveEntry(_entryID, rtbContent);
		}

		private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var tr = new TextRange(rtbContent.Document.ContentStart, rtbContent.Document.ContentEnd);
			_modified = (tr.Text.Length > 0);
		}

		private void mnuFile_Create_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new CreateDatabase();
			var result = dlg.ShowDialog();
			if (result.HasValue && result.Value)
			{
				
			}			
		}
	}
}
