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
using SrtStudio;

namespace TimespanFormatterTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int d, h, m, s, ms;
            d = Convert.ToInt32(textboxD.Text);
            h = Convert.ToInt32(textboxH.Text);
            m = Convert.ToInt32(textboxM.Text);
            s = Convert.ToInt32(textboxS.Text);
            ms = Convert.ToInt32(textboxMS.Text);
            TimeSpan timeSpan = new TimeSpan(d, h, m, s, ms);

            textboxOut.Text = timeSpan.ToShortForm();
        }
    }
}
