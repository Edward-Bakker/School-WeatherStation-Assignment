using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinalAssignment
{
    public partial class SplashScreen : Form
    {
        private MainWindow mainWindow;
        private Timer timer;

        public SplashScreen()
        {
            InitializeComponent();

            timer = new Timer { Interval = 5000 };
            timer.Tick += OnTick;
            timer.Start();

            mainWindow = new MainWindow();
            mainWindow.LoadOptionsFromDB();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (timer.Enabled)
                timer.Stop();

            mainWindow.Show();
            Hide();
        }
    }
}
