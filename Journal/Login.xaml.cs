using System.Windows;

namespace AdamOneilSoftware
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void tbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            btnOK.IsEnabled = (tbPassword.Password.Length > 0);
        }
    }
}