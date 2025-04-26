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
using System.Windows.Shapes;

namespace ProcessWatcher
{
    /// <summary>
    /// Логика взаимодействия для EnterBannApp.xaml
    /// </summary>
    public partial class EnterBannApp : Window
    {
        public BannApp BannApp { get; private set; }

        public EnterBannApp(BannApp bannApp)
        {
            InitializeComponent();
            BannApp = bannApp;
            DataContext = BannApp;
        }

        void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}
