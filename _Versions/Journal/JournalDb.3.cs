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
								[SameDayIndex] int NOT NULL DEFAULT (0),							
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
					// verify it opens, and set multi-entry days for proper display indexing
					using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
					{
						cn.Open();
						SetSameDayIndexes(cn);
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

		private void SetSameDayIndexes(SqlCeConnection cn)
		{			
			var days = cn.Query<MultiEntryDay>(
				@"SELECT 
					DATEPART(yy, [Date]) AS [Year],
					DATEPART(m, [Date]) AS [Month],
					DATEPART(d, [Date]) AS [Day],
					COUNT(1) AS [Count]
				FROM
					[JournalEntry]
				GROUP BY
					DATEPART(yy, [Date]),
					DATEPART(m, [Date]),
					DATEPART(d, [Date])
				HAVING
					COUNT(1)>1", null);
				
			foreach (var day in days)
			{
				var entries = cn.Query<int>(
					@"SELECT [ID] FROM [JournalEntry] 
					WHERE DATEPART(yy, [Date])=@year AND DATEPART(m, [Date])=@month AND DATEPART(d, [Date])=@day
					ORDER BY [Date]", 
					new { year = day.Year, month = day.Month, day = day.Day });
				int index = 0;
				foreach (var entryID in entries)
				{
					index++;
					cn.Execute("UPDATE [JournalEntry] SET [SameDayIndex]=@index WHERE [ID]=@id", new { index = index, id = entryID });
				}					
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
			Entry result = null;			

			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
				result = cn.Query<Entry>("SELECT * FROM [JournalEntry] WHERE [ID]=@ID", new { ID = id }).SingleOrDefault();
				cn.Close();
			}
			
			return result;
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
				new { Aggregate = "MAX", Operator = "<" }, // previous [0]
				new { Aggregate = "MIN", Operator = ">" } // next [1]
			};

			int? result = null;
			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
				
				if (id != 0)
				{
					result = cn.Query<int?>($"SELECT {queryTokens[(int)direction].Aggregate}([ID]) FROM [JournalEntry] WHERE [ID]{queryTokens[(int)direction].Operator}@id", new { id = id }).SingleOrDefault();
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

		public int SameDayEntryIndex(int id)
		{
			int result = 0;

			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();

			}

			return result;
		}

		public IEnumerable<EntryHeader> Search(string query)
		{
			using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
			{
				cn.Open();
			}
		}

		public IEnumerable<EntryHeader> EntryHeaders
		{
			get
			{				
				using (SqlCeConnection cn = new SqlCeConnection(_connectionString))
				{
					cn.Open();
					return cn.Query<EntryHeader>("SELECT [Date], [SameDayIndex], [ID] FROM [JournalEntry] ORDER BY [Date] DESC", null);
				}
			}
		}

		public class EntryHeader
		{
			public DateTime Date { get; set; }
			public int SameDayIndex { get; set; }
			public string SampleText { get; set; }
			public int ID { get; set; }

			public string DisplayDate
			{
				get
				{
					string result = Date.ToString("ddd M/d/yy");
					if (SameDayIndex > 0) result += $":{SameDayIndex}";
					return result;
				}
			}

			public string DisplayGroup
			{
				get { return string.Format("{0:MMM yyyy}", Date); }
			}
		}

		public class Entry
		{
			public DateTime Date { get; set; }
			public int SameDayIndex { get; set; }			
			public string RichText { get; set; }
			public string PlainText { get; set; }
			public int Revision { get; set; }			
			public int ID { get; set; }

			public string DisplayDate
			{
				get
				{
					string result = Date.ToString("ddd M/d/yy");
					if (SameDayIndex > 0) result += $":{SameDayIndex}";
					return result;
				}
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
