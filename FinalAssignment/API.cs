using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FinalAssignment
{
    class API
    {
        private string apiKey;
        public string unit = "metric";

        public API(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public string GetCityInfo(string city)
        {
            string url = string.Format("https://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units={2}", city, apiKey, unit);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            try
            {
                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
