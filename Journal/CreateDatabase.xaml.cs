using System;
using System.IO;
using System.Windows;

namespace AdamOneilSoftware
{
    /// <summary>
    /// Interaction logic for CreateDatabase.xaml
    /// </summary>
    public partial class CreateDatabase : Window
    {
        public CreateDatabase()
        {
            InitializeComponent();
        }

        public bool IsValidPassword(out string message)
        {
            message = null;
            if (tbPassword.Password.Equals(tbConfirmPwd.Password))
            {
                if (tbPassword.Password.Length >= 4) return true;
                message = "Password must be at least 4 characters";
            }
            message = "Passwords don't match.";
            return false;
        }

        public string Password
        {
            get { return tbPassword.Password; }
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrEmpty(tbFilename.Text))
            {
                message = "Please select filename.";
                return false;
            }

            if (!IsValidPassword(out message))
            {
                return false;
            }

            message = null;
            return true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            string message;
            if (IsValid(out message))
            {
                if (chkSavePwd.IsChecked.Value)
                {
                    string pwdFile = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        System.IO.Path.GetFileName(tbFilename.Text) + ".txt");

                    using (StreamWriter writer = File.CreateText(pwdFile))
                    {
                        writer.WriteLine("Password for journal file {0}: {1}", tbFilename.Text, tbPassword.Password);
                        writer.Close();
                    }
                }
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(message);
            }
        }
    }
}