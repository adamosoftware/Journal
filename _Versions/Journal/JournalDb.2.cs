using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Dapper;

namespace AdamOneilSoftware
{
	public enum AdjacentEntryDirection
	{
		Previous = 0,
		Next = 1
	}

	class JournalDb
	{		
		private SqlCeEngine _engine;
		private string _connectionString = null;
		private bool _opened = false;

		public JournalDb()
		{
		}

		public bool Open(string fileName, string password)
		{
			_opened = false;
			try
			{
				_connectionString = string.Format($"Data Source='{fileName}';LCID=1033;Password={password};Encryption Mode=Platform Default");
				_engine = new SqlCeEngine(_connectionString);

				if (!File.Exists(fileName))
				{
					string folder = Path.GetDirectoryName(fileName);
					if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

					_engine.LocalConnectionString = _connectionString;
					_engine.CreateDatabase();

					using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
					{
						cn.Open();
						SqlCeCommand cmd = new SqlCeCommand(
							@"CREATE TABLE [JournalEntry] (
								[Date] datetime NOT NULL,								
								[RichText] ntext NOT NULL,
								[PlainText] ntext NOT NULL,
								[Revision] int NOT NULL,
								[DateModified] datetime NULL,
								[ID] int identity(1,1) PRIMARY KEY
							)", cn);
						cmd.ExecuteNonQuery();
						cn.Close();
					}					
				}
				else
				{
					// verify it opens
					using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
					{
						cn.Open();
						cn.Close();
					}
				}
				_opened = true;
				return true;
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message);
				return false;
			}
		}

		private string OneDriveFolder
		{
			get
			{
				string[] regKeys = new string[] {
					"HKEY_CURRENT_USER\\Software\\Microsoft\\SkyDrive",
					"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\SkyDrive",
					"HKEY_CURRENT_USER\\Software\\Microsoft\\OneDrive"
				};
				foreach (var regKey in regKeys)
				{
					var folder = Registry.GetValue(regKey, "UserFolder", null);
					if (folder != null && Directory.Exists(folder.ToString())) return folder.ToString();
				}
				throw new Exception("Couldn't find OneDrive folder.");
			}
		}

		public bool IsOpened
		{
			get { return _opened; }
		}

		public void SaveEntry(int entryID, RichTextBox rtbContent)
		{
			var content = new TextRange(rtbContent.Document.ContentStart, rtbContent.Document.ContentEnd);
			if (content.IsEmpty) return;

			string richText = null;
			using (var ms = new MemoryStream())
			{
				content.Save(ms, DataFormats.Rtf);
				ms.Position = 0;
				using (StreamReader reader = new StreamReader(ms))
				{
					richText = reader.ReadToEnd();
					reader.Close();
				}
				ms.Close();
			}

			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
				using (SqlCeCommand cmd = (entryID == 0) ?
					new SqlCeCommand("INSERT INTO [JournalEntry] ([Date], [RichText], [PlainText], [Revision]) VALUES (@Date, @RichText, @PlainText, 0)", cn) :
					new SqlCeCommand("UPDATE [JournalEntry] SET [DateModified]=@Date, [RichText]=@RichText, [PlainText]=@PlainText, [Revision]=[Revision]+1 WHERE [ID]=@ID", cn))
				{
					if (entryID != 0) cmd.Parameters.Add(new SqlCeParameter("ID", entryID));
					cmd.Parameters.Add(new SqlCeParameter("Date", DateTime.Now));
					cmd.Parameters.Add(new SqlCeParameter("RichText", richText));
					cmd.Parameters.Add(new SqlCeParameter("PlainText", content.Text));

					cmd.ExecuteNonQuery();
				}
				cn.Close();
			}
		}

		public Entry Find(int id)
		{
			DataTable tbl = new DataTable();
			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
				using (SqlCeCommand cmd = new SqlCeCommand("SELECT * FROM [JournalEntry] WHERE [ID]=@ID", cn))
				{
					cmd.Parameters.AddWithValue("ID", id);
					SqlCeDataAdapter adapter = new SqlCeDataAdapter(cmd);
					adapter.Fill(tbl);
				}
				cn.Close();
			}
			if (tbl.Rows.Count == 1) return new Entry(tbl.Rows[0]);
			return null;
		}

		public int? PreviousEntryID(int id)
		{
			return AdjacentEntryID(id, AdjacentEntryDirection.Previous);
		}

		public int? NextEntryID(int id)
		{
			return AdjacentEntryID(id, AdjacentEntryDirection.Next);
		}

		private int? AdjacentEntryID(int id, AdjacentEntryDirection direction)
		{
			var queryTokens = new[]
			{
				new { Aggregate = "MAX", Operator = "<" }, // previous
				new { Aggregate = "MIN", Operator = ">" } // next
			};

			int? result = null;
			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
				
				if (id != 0)
				{
					result = cn.Query<int?>($"SELECT {queryTokens[(int)direction]}([ID]) FROM [JournalEntry] WHERE [ID]{queryTokens[(int)direction]}@id", new { id = id }).SingleOrDefault();
				}
				else
				{
					if (direction == AdjacentEntryDirection.Previous)
					{
						result = cn.Query<int?>("SELECT MAX([ID]) FROM [JournalEntry]").SingleOrDefault();
					}
				}										
			}
			return result;
		}

		public Entry PreviousEntry(int id)
		{
			int? prevID = AdjacentEntryID(id, AdjacentEntryDirection.Previous);
			if (prevID.HasValue) return Find(prevID.Value);
			return null;
		}

		public Entry NextEntry(int id)
		{
			int? nextID = AdjacentEntryID(id, AdjacentEntryDirection.Next);
			if (nextID.HasValue) return Find(nextID.Value);
			return null;
		}

		public IEnumerable<EntryHeader> EntryHeaders
		{
			get
			{
				DataTable tbl = new DataTable();
				using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
				{
					cn.Open();
					using (SqlCeCommand cmd = new SqlCeCommand("SELECT * FROM [JournalEntry] ORDER BY [Date] DESC", cn))
					{
						SqlCeDataAdapter adapter = new SqlCeDataAdapter(cmd);
						adapter.Fill(tbl);
					}
					cn.Close();
				}
				return tbl.AsEnumerable().Select(dataRow => new EntryHeader(dataRow));
			}
		}

		public class EntryHeader
		{
			public DateTime Date { get; set; }
			public int ID { get; set; }

			public string DisplayDate
			{
				get { return Date.ToString("ddd M/d"); }
			}

			public string DisplayGroup
			{
				get { return string.Format("{0:MMM yyyy}", Date); }
			}

			public EntryHeader(DataRow dataRow)
			{
				Date = dataRow.Field<DateTime>("Date");
				ID = dataRow.Field<int>("ID");
			}
		}

		public class Entry
		{
			public DateTime Date { get; set; }
			public string RichText { get; set; }
			public string PlainText { get; set; }
			public int Revision { get; set; }
			public int ID { get; set; }

			public string DisplayDate
			{
				get { return Date.ToString("ddd M/d/yy"); }
			}

			public Entry()
			{
			}

			public Entry(DataRow dataRow)
			{
				Date = dataRow.Field<DateTime>("Date");
				RichText = dataRow.Field<string>("RichText");
				PlainText = dataRow.Field<string>("PlainText");
				Revision = dataRow.Field<int>("Revision");
				ID = dataRow.Field<int>("ID");
			}
		}
	}

	internal class TreeViewEntryItem : TreeViewItem
	{
		public int ID { get; set; }
	}
}
