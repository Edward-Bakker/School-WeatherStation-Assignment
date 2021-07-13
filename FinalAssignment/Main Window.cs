using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace FinalAssignment
{
    public partial class MainWindow : Form
    {
        private string city = "Emmen";
        private string unit = "metric";
        private int interval = 60;

        private Timer timer;
        private API api;

        public MainWindow()
        {
            InitializeComponent();
            // Insert your API key here as string
            api = new API("9c5d15dce2fa2aba46c9cc8dfcc718ca");
            StartTimer();
            UpdateWeather();
        }

        private SqlConnection ConnectToDatabase()
        {
            string sqlConnectionString = @"Data Source = (localdb)\MSSQLLocalDB;Database = StendenWeatherstation";

            return new SqlConnection(sqlConnectionString);
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                HandleBalloonTip("The Stenden weatherstation is now running in the background!");
            }
            else if (WindowState == FormWindowState.Normal)
            {
                notifyIcon.Visible = false;
                ShowInTaskbar = true;
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                e.Cancel = true;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().Show();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateWeather();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            tabControl1.SelectedTab = tabPage3;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            bool isMetric = metricRadioButton.Checked;

            city = cityTextBox.Text;
            api.unit = (isMetric) ? "metric" : "imperial";
            unit = (isMetric) ? "metric" : "imperial";
            interval = (int)intervalNumericUpDown.Value;

            UpdateWeather();
            SetOptionsInDB();
        }

        private void StartTimer()
        {
            // Time in seconds multiplied by conversion from seconds to ms
            timer = new Timer { Interval = interval * 1000 };
            timer.Tick += OnTick;
            timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (timer.Enabled)
                timer.Stop();

            UpdateWeather();
            StartTimer();
        }

        private void UpdateWeather()
        {

            try
            {
                UpdateLabels(JsonDocument.Parse(api.GetCityInfo(city)));
            }
            catch (Exception)
            {
                HandleBalloonTip("Something went wrong, do you have an internet connection and is openweathermap online?");
            }
            LoadWeatherFromDB();
        }

        private void UpdateLabels(JsonDocument jsonResponse)
        {

            try
            {
                JsonElement root = jsonResponse.RootElement;
                JsonElement main = root.GetProperty("main");
                JsonElement wind = root.GetProperty("wind");

                string tempString = $"Current temperature: {main.GetProperty("temp").GetDecimal()} ";
                if (unit == "metric")
                    tempString += "°C";
                else if (unit == "imperial")
                    tempString += "°F";

                weatherPictureBox.Load(GetIconUrl(root));
                cityLabel.Text = root.GetProperty("name").GetString();
                weatherLabel.Text = root.GetProperty("weather")[0].GetProperty("main").GetString();
                temperatureLabel.Text = tempString;
                pressureLabel.Text = $"Pressure: {main.GetProperty("pressure").GetInt32()} hPa";
                humidityLabel.Text = $"Humidity: {main.GetProperty("humidity").GetInt32()} %";
                if (unit == "metric")
                    windLabel.Text = $"Windspeed: {wind.GetProperty("speed").GetDecimal()} m/s, heading {GetWindDirection(wind.GetProperty("deg").GetInt32())}";
                else
                    windLabel.Text = $"Windspeed: {wind.GetProperty("speed").GetDecimal()} mph, heading {GetWindDirection(wind.GetProperty("deg").GetInt32())}";
                temperatureToolStripMenuItem.Text = tempString;
                lastUpdateLabel.Text = DateTime.Now.ToString();

                SetWeatherInDB(main.GetProperty("temp").GetDecimal());
            }
            catch (Exception e)
            {
                HandleBalloonTip(e.Message);
            }
        }

        private string GetIconUrl(JsonElement root)
        {
            string iconName = root.GetProperty("weather")[0].GetProperty("icon").GetString();
            return $"http://openweathermap.org/img/wn/{iconName}@2x.png";
        }

        private string GetWindDirection(int degree)
        {
            string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
            return cardinals[(int)Math.Round((double)degree % 360 / 45)];
        }

        private void SetOptionsInDB()
        {
            try
            {
                SqlConnection sqlConnection = ConnectToDatabase();

                SqlCommand deleteCommand = sqlConnection.CreateCommand();
                deleteCommand.CommandText = "delete from Settings";

                SqlCommand insertCommand = sqlConnection.CreateCommand();
                insertCommand.CommandText = "insert into Settings (city, unit, interval) values (@param1, @param2, @param3)";
                insertCommand.Parameters.AddWithValue("@param1", SqlDbType.NVarChar).Value = city;
                insertCommand.Parameters.AddWithValue("@param2", SqlDbType.NVarChar).Value = unit;
                insertCommand.Parameters.AddWithValue("@param3", SqlDbType.Int).Value = interval;

                sqlConnection.Open();

                deleteCommand.ExecuteNonQuery();
                insertCommand.ExecuteNonQuery();

                deleteCommand.Dispose();
                insertCommand.Dispose();
                sqlConnection.Close();
            }
            catch (Exception e)
            {
                HandleBalloonTip(e.Message);
            }
        }

        public void LoadOptionsFromDB()
        {
            try
            {
                SqlConnection sqlConnection = ConnectToDatabase();

                SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandText = "select city, unit, interval from Settings";

                sqlConnection.Open();

                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
                    if (sqlDataReader.Read())
                    {
                        city = sqlDataReader.GetString(0);
                        unit = sqlDataReader.GetString(1);
                        interval = sqlDataReader.GetInt32(2);
                    }
                    sqlDataReader.Close();
                }
                sqlCommand.Dispose();
                sqlConnection.Close();

                cityTextBox.Text = city;
                metricRadioButton.Checked = (unit == "metric");
                imperialRadioButton.Checked = (unit == "imperial");
                intervalNumericUpDown.Value = interval;
                api.unit = unit;

                UpdateWeather();
            }
            catch (Exception e)
            {
                HandleBalloonTip(e.Message);
            }
        }

        private void LoadWeatherFromDB()
        {
            foreach (Series series in weatherChart.Series)
                series.Points.Clear();

            try
            {
                SqlConnection sqlConnection = ConnectToDatabase();

                SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandText = "select ROUND(AVG(temperature), 0), CAST(date AS DATE) as DateField from APIData WHERE city = @param1 AND unit = @param2 GROUP BY CAST(date AS DATE)";
                sqlCommand.Parameters.AddWithValue("@param1", SqlDbType.NVarChar).Value = city;
                sqlCommand.Parameters.AddWithValue("@param2", SqlDbType.NVarChar).Value = unit;

                sqlConnection.Open();

                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        weatherChart.Series["AverageTemperature"].Points.AddXY(sqlDataReader.GetValue(1), sqlDataReader.GetValue(0));
                    }
                    sqlDataReader.Close();
                }
                sqlCommand.Dispose();
                sqlConnection.Close();
            }
            catch (Exception e)
            {
                HandleBalloonTip(e.Message);
            }
        }

        private void SetWeatherInDB(decimal temperature)
        {
            try
            {
                SqlConnection sqlConnection = ConnectToDatabase();

                SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandText = "insert into APIData (city, temperature, date, unit) values (@param1, @param2, @param3, @param4)";
                sqlCommand.Parameters.AddWithValue("@param1", SqlDbType.NVarChar).Value = city;
                sqlCommand.Parameters.AddWithValue("@param2", SqlDbType.Float).Value = (float)temperature;
                sqlCommand.Parameters.AddWithValue("@param3", SqlDbType.DateTime).Value = DateTime.Now;
                sqlCommand.Parameters.AddWithValue("@param4", SqlDbType.DateTime).Value = unit;

                sqlConnection.Open();

                sqlCommand.ExecuteNonQuery();

                sqlCommand.Dispose();
                sqlConnection.Close();

                RemoveOldWeatherInDB();
            }
            catch (Exception e)
            {
                HandleBalloonTip(e.Message);
            }
        }

        private void RemoveOldWeatherInDB()
        {
            SqlConnection sqlConnection = ConnectToDatabase();

            SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "delete from APIData where date < GETDATE() - 4";

            sqlConnection.Open();

            sqlCommand.ExecuteNonQuery();

            sqlCommand.Dispose();
            sqlConnection.Close();
        }

        private void HandleBalloonTip(string message, int interval = 2000)
        {
            if (!notifyIcon.Visible)
                notifyIcon.Visible = true;

            notifyIcon.BalloonTipText = message;
            notifyIcon.ShowBalloonTip(2000);
        }
    }
}
