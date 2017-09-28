using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using static AdamOneilSoftware.JournalDb;

namespace AdamOneilSoftware
{
	public partial class MainWindow : Window
	{
		private JournalDb _db = new JournalDb();
		private Options _options = null;

		private bool _entryModified = false;
		private int _entryID = 0;
		private int _prevEntryID = 0;
		private int _nextEntryID = 0;

		private PanelHandler _searchPanel = null;
		private PanelHandler _browserPanel = null;

		public MainWindow()
		{
			InitializeComponent();
			_searchPanel = new PanelHandler(splitterRight, RightSidebar) { TogglerMenu = mnuViewSearch };
			_browserPanel = new PanelHandler(splitterLeft, LeftSidebar) { TogglerMenu = mnuViewBrowser };
		}

		private void mnuFileNew_Click(object sender, RoutedEventArgs e)
		{
			CreateDatabase dlg = new CreateDatabase();
			if (dlg.ShowDialog().Value)
			{
				if (_db.Open(dlg.tbFilename.Text, dlg.tbPassword.Password)) ShowFilename(dlg.tbFilename.Text);
			}
		}

		private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Journal Databases|*.sdf|All Files|*.*";
			if (dlg.ShowDialog().Value)
			{
				Login(dlg.FileName);
			}			
		}

		private void Login(string fileName)
		{
			while (true)
			{
				Login loginDlg = new Login();
				if (loginDlg.ShowDialog().Value)
				{
					if (_db.Open(fileName, loginDlg.tbPassword.Password))
					{
						ShowFilename(fileName);
						LoadBrowser();
						UpdatePrevNextLinks();
						break;
					}
					else
					{
						currentFilename.Text = "not logged in -- click Journal -> Open";
					}
				}
				else
				{
					currentFilename.Text = "not logged in -- click Journal -> Open";
					break;
				}
			}
		}

		private void UpdatePrevNextLinks()
		{
			_prevEntryID = 0;
			Entry prev = _db.PreviousEntry(_entryID);
			sbiPrevLink.Visibility = (prev != null) ? Visibility.Visible : Visibility.Collapsed;
			if (prev != null)
			{
				_prevEntryID = prev.ID;
				btnPrev.Content = "<< " + prev.DisplayDate;
			}

			_nextEntryID = 0;
			Entry next = _db.NextEntry(_entryID);			
			if (next != null)
			{
				_nextEntryID = next.ID;
				btnNext.Content = next.DisplayDate + " >>";
			}
			else
			{
				sbiNextLink.Visibility = (_entryID != 0) ? Visibility.Visible : Visibility.Collapsed;
				btnNext.Content = "New Entry";
			}

			//sbiPrevNextSeparator.Visibility = (_prevEntryID != 0 && _nextEntryID != 0) ? Visibility.Visible : Visibility.Collapsed;
		}

		private void ShowFilename(string fileName)
		{
			rtbContent.IsEnabled = true;
			rtbContent.Focus();

			//this.Title = "Journal - " + fileName;
			currentFilename.Text = fileName;
			_options.LastFileName = fileName;
			_options.Save();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_searchPanel.Collapse();
			_browserPanel.Collapse();

			_options = UserOptionsBase.Load<Options>("Options.xml");
			currentFilename.Text = _options.LastFileName;
			if (File.Exists(_options.LastFileName)) Login(_options.LastFileName);

			this.DataContext = _db;

			if (_db.IsOpened) LoadBrowser();

			mnuViewStatusBar.IsChecked = _options.ViewStatusBar;
			statusBar.Visibility = (_options.ViewStatusBar) ? Visibility.Visible : Visibility.Collapsed;
		}

		private void LoadBrowser()
		{
			tvwBrowser.Items.Clear();
			var headers = _db.EntryHeaders;

			var years = headers.GroupBy(item => item.Date.Year);
			foreach (var year in years)
			{
				TreeViewItem yearNode = new TreeViewItem();
				yearNode.Header = $"{year.Key} ({year.Count()})";
				tvwBrowser.Items.Add(yearNode);

				var months = headers.Where(item => item.Date.Year == year.Key).GroupBy(item => item.DisplayGroup);
				foreach (var month in months)
				{
					TreeViewItem parent = new TreeViewItem();
					parent.Header = $"{month.Key} ({month.Count()})";
					yearNode.Items.Add(parent);

					foreach (var entry in month)
					{
						TreeViewEntryItem child = new TreeViewEntryItem();
						child.Header = entry.DisplayDate;
						child.ID = entry.ID;
						parent.Items.Add(child);
					}
				}
			}
		}

		private void mnuFileSaveAndExit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveEntry();
		}

		private void SaveEntry()
		{
			if (_db.IsOpened && _entryModified)
			{
				_db.SaveEntry(_entryID, rtbContent);
				_entryModified = false;
			}
		}

		private void rtbContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			_entryModified = true;
		}

		private void tvwBrowser_SelectedItemChanged<T>(object sender, RoutedPropertyChangedEventArgs<T> e)
		{
			var item = e.NewValue as TreeViewEntryItem;
			if (item != null)
			{
				LoadEntry(item.ID);
			}
		}

		private void LoadEntry(int id)
		{
			SaveEntry();

			JournalDb.Entry entry = _db.Find(id);
			if (entry != null)
			{
				_entryID = entry.ID;
				UpdatePrevNextLinks();
				Title = $"Journal - {entry.DisplayDate}";
				var contentBytes = Encoding.ASCII.GetBytes(entry.RichText);
				using (MemoryStream ms = new MemoryStream(contentBytes))
				{
					rtbContent.SelectAll();
					rtbContent.Selection.Load(ms, DataFormats.Rtf);
					ms.Close();
					_entryModified = false;
				}
			}
		}

		private void mnuFileNewEntry_Click(object sender, RoutedEventArgs e)
		{
			NewEntry();
		}

		private void NewEntry()
		{
			SaveEntry();
			_entryID = 0;
			rtbContent.Document = new FlowDocument();
			UpdatePrevNextLinks();
			Title = "Journal - New Entry";
		}

		private void mnuViewStatusBar_Click(object sender, RoutedEventArgs e)
		{
			mnuViewStatusBar.IsChecked = !mnuViewStatusBar.IsChecked;
			statusBar.Visibility = (mnuViewStatusBar.IsChecked) ? Visibility.Visible : Visibility.Collapsed;
			_options.ViewStatusBar = mnuViewStatusBar.IsChecked;
			_options.Save();
		}

		private void btnNext_Click(object sender, RoutedEventArgs e)
		{
			if (_nextEntryID != 0)
			{
				LoadEntry(_nextEntryID);
			}
			else
			{
				NewEntry();
			}
		}

		private void btnPrev_Click(object sender, RoutedEventArgs e)
		{
			if (_prevEntryID != 0) LoadEntry(_prevEntryID);
		}

		private void btnSearch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_db.Search(tbSearch.Text);
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message);
			}
		}
	}
}
