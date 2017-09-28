﻿using Microsoft.Win32;
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

namespace AdamOneilSoftware
{
	public partial class MainWindow : Window
	{
		private JournalDb _db = new JournalDb();
		private Options _options = null;

		private bool _entryModified = false;
		private int _entryID = 0;

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
						break;
					}
				}
				else
				{
					break;
				}
			}
		}

		private void ShowFilename(string fileName)
		{
			rtbContent.IsEnabled = true;
			rtbContent.Focus();

			this.Title = "Journal - " + fileName;
			_options.LastFileName = fileName;
			_options.Save();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_searchPanel.Collapse();
			_browserPanel.Collapse();

			_options = UserOptionsBase.Load<Options>("Options.xml");
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
				SaveEntry();
				
				JournalDb.Entry entry = _db.Find(item.ID);
				if (entry != null)
				{
					_entryID = entry.ID;
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
		}

		private void mnuFileNewEntry_Click(object sender, RoutedEventArgs e)
		{
			SaveEntry();
			_entryID = 0;
			rtbContent.Document = new FlowDocument();
		}

		private void mnuViewStatusBar_Click(object sender, RoutedEventArgs e)
		{
			mnuViewStatusBar.IsChecked = !mnuViewStatusBar.IsChecked;
			statusBar.Visibility = (mnuViewStatusBar.IsChecked) ? Visibility.Visible : Visibility.Collapsed;
			_options.ViewStatusBar = mnuViewStatusBar.IsChecked;
			_options.Save();
		}
	}
}
