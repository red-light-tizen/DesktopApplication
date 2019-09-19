/*
 *  DataRegister.cs  
 *  
 *  Created on :Sep 15, 2019
 *  Author : Kasania
 */
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace RedLightDesktopUWP
{

    enum DataSeq
    {
        DataSeqYear = 0, DataSeqDay, DataSeqTime, DataSeqCondition, DataSeqPulse, DataSeqTemp, DataSeqSPO2, DataSeqActivity, DataSeqLatitude, DataSeqLongitude
    }

    class DataRegister
    {

        public FontIcon conditionIcon { get; private set; }
        public TextBlock conditionText { get; private set; }
        public TextBlock pulseText { get; private set; }
        public TextBlock TempText { get; private set; }
        public TextBlock SPO2Text { get; private set; }
        public TextBlock LocationText { get; private set; }

        private const string ConditionStringGood = "좋음";
        private const string ConditionStringCaution = "주의";
        private const string ConditionStringDanger = "위험";
        private const string ConditionStringNoConnection = "No Connection";

        private const string ConditionIconGood = "\uE76E";
        private const string ConditionIconCaution = "\uE814";
        private const string ConditionIconDanger = "\uEA39";
        private const string ConditionIconNoConnection = "\uF384";

        private const int geolocationUpdateFreq = 50;
        private static int geolocationUpdateCount = 0;

        //Remove this on Commit!!!
        private const string apiToken = "GoogleGeoAPI";

        private SolidColorBrush ConditionColorGood;
        private SolidColorBrush ConditionColorCaution;
        private SolidColorBrush ConditionColorDanger;
        private SolidColorBrush ConditionColorNoConnection;

        public DataRegister()
        {
            ConditionColorGood = new SolidColorBrush(Colors.LightGreen);
            ConditionColorCaution = new SolidColorBrush(Colors.Yellow);
            ConditionColorDanger = new SolidColorBrush(Colors.Red);
            ConditionColorNoConnection = new SolidColorBrush(Colors.Gray);
        }

        public DataRegister SetConditionFontIcon(ref FontIcon fontIcon)
        {
            conditionIcon = fontIcon;
            return this;
        }

        public DataRegister SetConditionTextBox(ref TextBlock textBox)
        {
            conditionText = textBox;
            return this;
        }
        

        public DataRegister SetPulseTextBox(ref TextBlock textBox)
        {
            pulseText = textBox;
            return this;
        }

        public DataRegister SetTempTextBox(ref TextBlock textBox)
        {
            TempText = textBox;
            return this;
        }
        public DataRegister SetSPO2TextBox(ref TextBlock textBox)
        {
            SPO2Text = textBox;
            return this;
        }
        public DataRegister SetLocationTextBox(ref TextBlock textBox)
        {
            LocationText = textBox;
            return this;
        }

        public async void UpdateData(String data)
        {
            string[] datas = data.Split(";");
            pulseText.Text = Convert.ToString(Convert.ToInt32(datas[(int)DataSeq.DataSeqPulse]));
            TempText.Text = Convert.ToString(Convert.ToDouble(datas[(int)DataSeq.DataSeqTemp]) /100 );
            SPO2Text.Text = Convert.ToString(Convert.ToDouble(datas[(int)DataSeq.DataSeqSPO2]) /100);

            switch (Convert.ToInt32(datas[(int)DataSeq.DataSeqCondition]))
            {
                case 0:
                    conditionIcon.Glyph = ConditionIconGood;
                    conditionText.Text = ConditionStringGood;

                    conditionIcon.Foreground = ConditionColorGood;
                    conditionText.Foreground = ConditionColorGood;
                    break;
                case 1:
                    conditionIcon.Glyph = ConditionIconCaution;
                    conditionText.Text = ConditionStringCaution;

                    conditionIcon.Foreground = ConditionColorCaution;
                    conditionText.Foreground = ConditionColorCaution;
                    break;
                case 2:
                    conditionIcon.Glyph = ConditionIconDanger;
                    conditionText.Text = ConditionStringDanger;

                    conditionIcon.Foreground = ConditionColorDanger;
                    conditionText.Foreground = ConditionColorDanger;
                    break;

                default:
                    conditionIcon.Glyph = ConditionIconNoConnection;
                    conditionText.Text = ConditionStringNoConnection;

                    conditionIcon.Foreground = ConditionColorNoConnection;
                    conditionText.Foreground = ConditionColorNoConnection;
                    break;
            }

            //Delete when Commit

            geolocationUpdateCount -= 1;

            if (geolocationUpdateCount < 0)
            {
                geolocationUpdateCount = geolocationUpdateFreq;
                double lat = Convert.ToDouble(datas[(int)DataSeq.DataSeqLatitude]);
                double lng = Convert.ToDouble(datas[(int)DataSeq.DataSeqLongitude]);

                string query = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&language=ko&key={apiToken}";
                string location = "검색 중...";
                try
                {

                
                await Task.Run(() =>
                {
                    WebRequest request = WebRequest.Create(query);

                    WebResponse response = request.GetResponse();

                    Stream responseData = response.GetResponseStream();

                    StreamReader reader = new StreamReader(responseData);

                    // json-formatted string from maps api
                    string responseFromServer = reader.ReadToEnd();

                    JObject jObject = JObject.Parse(responseFromServer);
                    location = "";
                    for (int i = 3; i >= 0; --i)
                    {
                        try
                        {
                            location = location + "\n" + jObject["results"][0]["address_components"][i]["long_name"].ToString();
                        }
                        catch (Exception)
                        {
                            LocationText.Text = "APIRequest\nError";
                            continue;
                        }
                    }

                    response.Close();
                });

                LocationText.Text = location.Trim();
                }
                catch (Exception)
                {
                    LocationText.Text = "Check\nInternet\nconnection.";
                }
            }
        }

        
    }

    

}
