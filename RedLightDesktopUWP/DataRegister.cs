﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.UI.Xaml.Controls;

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

        private const int ConvertScaleGeo = 100000;

        public DataRegister()
        {
            
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
            String[] datas = data.Split(";");
            pulseText.Text = Convert.ToString(Convert.ToInt32(datas[(int)DataSeq.DataSeqPulse]));
            TempText.Text = Convert.ToString(Convert.ToDouble(datas[(int)DataSeq.DataSeqTemp]) /100 );
            SPO2Text.Text = Convert.ToString(Convert.ToDouble(datas[(int)DataSeq.DataSeqSPO2]) /100);

            switch (Convert.ToInt32(datas[(int)DataSeq.DataSeqCondition]))
            {
                case 0:
                    conditionIcon.Glyph = ConditionIconGood;
                    conditionText.Text = ConditionStringGood;
                    break;
                case 1:
                    conditionIcon.Glyph = ConditionIconCaution;
                    conditionText.Text = ConditionStringCaution;
                    break;
                case 2:
                    conditionIcon.Glyph = ConditionIconDanger;
                    conditionText.Text = ConditionStringDanger;
                    break;

                default:
                    conditionIcon.Glyph = ConditionIconNoConnection;
                    conditionText.Text = ConditionStringNoConnection;
                    break;
            }

            //Delete when Commit
            MapService.ServiceToken = "private token";

            Geopoint location = new Geopoint(new BasicGeoposition { Latitude = Convert.ToDouble(datas[(int)DataSeq.DataSeqLatitude])/ ConvertScaleGeo, Longitude = Convert.ToDouble(datas[(int)DataSeq.DataSeqLongitude]) / ConvertScaleGeo });
            MapLocationFinderResult locationInfo = await MapLocationFinder.FindLocationsAtAsync(location);

            if(locationInfo.Status == MapLocationFinderStatus.Success)
            {
                LocationText.Text = $"{locationInfo.Locations[0].Address.Town}";
            }
            else
            {
                LocationText.Text =$"{locationInfo.Status.ToString()}";
            }
            

        }


    }
}