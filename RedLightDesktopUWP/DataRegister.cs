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
        public FontIcon conditionIcon2 { get; private set; }
        public TextBlock conditionText { get; private set; }
        public TextBlock pulseText { get; private set; }
        public TextBlock TempText { get; private set; }
        public TextBlock SPO2Text { get; private set; }
        public TextBlock LocationText { get; private set; }

        private const string ConditionStringGood = "좋음";
        private const string ConditionStringCaution = "주의";
        private const string ConditionStringDanger = "위험";
        private const string ConditionStringUserAction = "사용자 요청";
        private const string ConditionStringNoConnection = "No Connection";

        private const string ConditionIconGood = "\uE76E";
        private const string ConditionIconCaution = "\uE814";
        private const string ConditionIconDanger = "\uEA39";
        private const string ConditionIconUserAction = "\uF13C";
        private const string ConditionIconUserActionBase = "\uEA18";
        private const string ConditionIconNoConnection = "\uF384";

        private const int geolocationUpdateFreq = 600;
        private static int geolocationUpdateCount = 0;

        //Remove this on Commit!!!
        private const string apiToken = "GoogleGeoAPI";
        string[] datas;

        private SolidColorBrush ConditionColorGood;
        private SolidColorBrush ConditionColorCaution;
        private SolidColorBrush ConditionColorDanger;
        private SolidColorBrush ConditionColorNoConnection;

        private int lastPulse;
        private double lastTemp;
        private double lastSPO2;

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

        public DataRegister SetConditionFontIcon2(ref FontIcon fontIcon)
        {
            conditionIcon2 = fontIcon;
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

            try
            {

            
            datas = data.Split(";");


            lastPulse = Convert.ToInt32(datas[(int)DataSeq.DataSeqPulse]);
            lastTemp = Convert.ToDouble(datas[(int)DataSeq.DataSeqTemp]) / 100.0;
            lastSPO2 = Convert.ToDouble(datas[(int)DataSeq.DataSeqSPO2]) / 100.0;

            }
            catch (Exception)
            {
                //when invalid data.
                return;
            }

            pulseText.Text = Convert.ToString(lastPulse);
            TempText.Text = Convert.ToString(lastTemp);
            SPO2Text.Text = Convert.ToString(lastSPO2);



            switch (Convert.ToInt32(datas[(int)DataSeq.DataSeqCondition]))
            {
                case 0:
                    conditionIcon.Glyph = ConditionIconGood;
                    conditionIcon2.Glyph = ConditionIconGood;
                    conditionText.Text = ConditionStringGood;

                    conditionIcon.Foreground = ConditionColorGood;
                    conditionIcon2.Foreground = ConditionColorGood;
                    conditionText.Foreground = ConditionColorGood;
                    geolocationUpdateCount -= 1;
                    break;
                case 1:
                    conditionIcon.Glyph = ConditionIconCaution;
                    conditionIcon2.Glyph = ConditionIconCaution;
                    conditionText.Text = ConditionStringCaution;

                    conditionIcon.Foreground = ConditionColorCaution;
                    conditionIcon2.Foreground = ConditionColorCaution;
                    conditionText.Foreground = ConditionColorCaution;
                    geolocationUpdateCount = 0;
                    break;
                case 2:
                    conditionIcon.Glyph = ConditionIconDanger;
                    conditionIcon2.Glyph = ConditionIconDanger;
                    conditionText.Text = ConditionStringDanger;

                    conditionIcon.Foreground = ConditionColorDanger;
                    conditionIcon2.Foreground = ConditionColorDanger;
                    conditionText.Foreground = ConditionColorDanger;
                    geolocationUpdateCount = 0;
                    break;
                case 3:
                    conditionIcon2.Glyph = ConditionIconUserAction;
                    conditionIcon.Glyph = ConditionIconUserActionBase;
                    conditionText.Text = ConditionStringUserAction;

                    conditionIcon.Foreground = ConditionColorDanger;
                    conditionIcon2.Foreground = ConditionColorDanger;
                    conditionText.Foreground = ConditionColorDanger;
                    geolocationUpdateCount -= 1;
                    break;

                default:
                    conditionIcon.Glyph = ConditionIconNoConnection;
                    conditionIcon2.Glyph = ConditionIconNoConnection;
                    conditionText.Text = ConditionStringNoConnection;

                    conditionIcon.Foreground = ConditionColorNoConnection;
                    conditionIcon2.Foreground = ConditionColorNoConnection;
                    conditionText.Foreground = ConditionColorNoConnection;
                    break;
            }


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
