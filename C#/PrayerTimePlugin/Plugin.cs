using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Rainmeter;

namespace PrayerTimePlugin
{
    class Measure
    {
        private static Settings CurrentSettings { get; set; }

        private static PrayerTime TodayPrayerTimes;

        private static DateTime CurrentDate;

        private static string DataFilePath;

        private static string HijriDate;

        class Settings
        {
            public string Latitude;
            public string Longitude;
            public string Month;
            public string Year;
            public int Method;
            public int School;
            public int MidnightMode;
            public int LatitudeAdjustmentMethod;
            public int HijriAdjustment;
            public bool _12HourMode;

            public void SetDateFromDateTime(DateTime date)
            {
                Month = date.Month.ToString();
                Year = date.Year.ToString();
            }
        }

        class Request
        {
            public int code { get; set; }
            public string status { get; set; }
            public List<Data> data { get; set; }
        }

        class Data
        {
            public PrayerTime timings { get; set; }
            public Date date { get; set; }
            public Meta meta { get; set; }
        }

        class PrayerTime {
            public string Fajr { get; set; }
            public string Sunrise { get; set; }
            public string Dhuhr { get; set; }
            public string Asr { get; set; }
            public string Sunset { get; set; }
            public string Maghrib { get; set; }
            public string Isha { get; set; }
            public string Imsak { get; set; }
            public string Midnight { get; set; }

            public override bool Equals(object obj)
            {
                PrayerTime toCompareWith = obj as PrayerTime;
                if (toCompareWith != obj)
                {
                    return toCompareWith.Fajr.Equals(Fajr) && toCompareWith.Sunrise.Equals(Sunrise) && toCompareWith.Sunset.Equals(Sunset) && toCompareWith.Dhuhr.Equals(Dhuhr)
                        && toCompareWith.Asr.Equals(Asr) && Maghrib.Equals(toCompareWith.Maghrib) && Isha.Equals(toCompareWith.Isha) && Imsak.Equals(toCompareWith.Imsak)
                        && Midnight.Equals(toCompareWith.Midnight);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }

        class Date
        {
            public string readable { get; set; }
            public string timestamp { get; set; }
            public Calendar gregorian { get; set; }
            public Calendar hijri { get; set; }

        }

        class Calendar
        {
            public string data { get; set; }
            public string format { get; set; }
            public string day { get; set; }
            public Weekday weekday { get; set; }
            public Month month { get; set; }
            public string year { get; set; }
            public Designation designation { get; set; }
            public List<object> holidays { get; set; }
        }

        class Weekday
        {
            public string en { get; set; }
            public string ar { get; set; }
        }

        class Month
        {
            public string number { get; set; }
            public string en { get; set; }
            public string ar { get; set; }
        }

        class Designation
        {
            public string abbreviated { get; set; }
            public string expanded { get; set; }
        }

        class Meta
        {
            public string latitude { get; set; }
            public string longitude { get; set; }
            public string timezone { get; set; }
            public string latitudeAdjustmentMethod { get; set; }
            public string midnightMode { get; set; }
            public string school { get; set; }
            public Offset offset { get; set; }
        }

        class Method
        {
            public int id { get; set; }
            public string name { get; set; }
            public MethodParams method { get; set; }
        }

        class MethodParams
        {
            public float Fajr { get; set; }
            public float Isha { get; set; }
        }

        public class Offset
        {
            public int Imsak { get; set; }
            public int Fajr { get; set; }
            public int Sunrise { get; set; }
            public int Dhuhr { get; set; }
            public int Asr { get; set; }
            public int Maghrib { get; set; }
            public int Sunset { get; set; }
            public int Isha { get; set; }
            public int Midnight { get; set; }
        }

        //http://api.aladhan.com/v1/calendar?latitude=51.508515&longitude=-0.1254872&method=2&month=4&year=2017


        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        public IntPtr buffer = IntPtr.Zero;


        public Measure(string path, string latitude, string longitude, int method, int school, int latitudeAdjustmentMethod, int hijriAdjustment, int midnightMode, bool _12HourClock)
        {
            CurrentSettings = new Settings();
            CurrentSettings.Latitude = latitude;
            CurrentSettings.Longitude = longitude;
            CurrentSettings.Method = method;
            CurrentSettings.School = school;
            CurrentSettings.LatitudeAdjustmentMethod = latitudeAdjustmentMethod;
            CurrentSettings.HijriAdjustment = hijriAdjustment;
            CurrentSettings.MidnightMode = midnightMode;
            CurrentSettings._12HourMode = _12HourClock;
            CurrentDate = DateTime.Today;
            CurrentSettings.Month = CurrentDate.Month.ToString();
            CurrentSettings.Year = CurrentDate.Year.ToString();
            DataFilePath = path;
            SetPrayerTimesData(false);
        }

        public void Update()
        {
            if (!CurrentDate.Date.Equals(DateTime.Today))
            {
                bool downloadNew = CurrentDate.Month != DateTime.Today.Month;
                CurrentDate = DateTime.Today;
                CurrentSettings.SetDateFromDateTime(CurrentDate);
                SetPrayerTimesData(downloadNew);
            }
        }

        public void Reload(string latitude, string longitude, int method, int school, int latitudeAdjustmentMethod, int hijriAdjustment, int midnightMode, bool _12HourClock)
        {
            if (latitude != CurrentSettings.Latitude || longitude != CurrentSettings.Longitude || method != CurrentSettings.Method || school != CurrentSettings.School 
                || latitudeAdjustmentMethod != CurrentSettings.LatitudeAdjustmentMethod || hijriAdjustment != CurrentSettings.HijriAdjustment 
                || midnightMode != CurrentSettings.MidnightMode)
            {
                CurrentSettings.Latitude = latitude;
                CurrentSettings.Longitude = longitude;
                CurrentSettings.Method = method;
                CurrentSettings.School = school;
                CurrentSettings.LatitudeAdjustmentMethod = latitudeAdjustmentMethod;
                CurrentSettings.HijriAdjustment = hijriAdjustment;
                CurrentSettings.MidnightMode = midnightMode;

                CurrentDate = DateTime.Today;
                CurrentSettings.SetDateFromDateTime(CurrentDate);
                SetPrayerTimesData(true);
            }

            if (_12HourClock != CurrentSettings._12HourMode)
            {
                CurrentSettings._12HourMode = _12HourClock;
                if (CurrentSettings._12HourMode)
                {
                    ConvertPrayerTimesTo12Hour();
                } else
                {
                    SetPrayerTimesData(false);
                }
            }
        }

        private void SetPrayerTimesData(bool downloadNew)
        {
            string data;
            Request request;
            if (!downloadNew)
            {
                data = GetPrayerTimeDataString();
                
                if (data != null)
                {
                    request = Newtonsoft.Json.JsonConvert.DeserializeObject<Request>(data);
                    if (request != null && IsStoredPrayerTimesSameAsSettings(request))
                    {
                        SetPrayerTimesFromRequest(request);
                        return;
                    }
                }
            }

            data = DownloadPrayerTimes();
            request = Newtonsoft.Json.JsonConvert.DeserializeObject<Request>(data);
            if (request != null)
            {
                SetPrayerTimesFromRequest(request);
            }
            if (data != null)
            {
                SavePrayerTimeDataString(data);
            }

        }

        private string DownloadPrayerTimes()
        {
            string result = null;
            string url = String.Format("http://api.aladhan.com/v1/calendar?latitude={0}&longitude={1}&method={2}&month={3}&year={4}&school={5}&midnightMode={6}&latitudeAdjustmentMethod={7}&adjustment={8}",
                CurrentSettings.Latitude, CurrentSettings.Longitude, CurrentSettings.Method, CurrentSettings.Month, CurrentSettings.Year, CurrentSettings.School, CurrentSettings.MidnightMode, CurrentSettings.LatitudeAdjustmentMethod, CurrentSettings.HijriAdjustment);
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                try
                {
                    result = client.DownloadString(url);
                } catch (Exception)
                {

                }
            }
            return result;
        }

        private void SavePrayerTimeDataString(string data)
        {
            if (DataFilePath == null) return;

            bool isFile = System.IO.File.Exists(DataFilePath);
            bool isDirectory = System.IO.Directory.Exists(DataFilePath);

            if (isFile || isDirectory)
            {
                string finalPath = null;

                if (isFile)
                {
                    //Get parent directory
                    string directory = System.IO.Path.GetDirectoryName(DataFilePath);
                    if (directory != null && !directory.Equals(String.Empty))
                    {
                        finalPath = System.IO.Path.Combine(directory, "prayertimes.json");
                    }
                }
                else if (isDirectory)
                {
                    finalPath = System.IO.Path.Combine(DataFilePath, "prayertimes.json");
                }

                if (finalPath != null || !finalPath.Equals(String.Empty))
                {
                    if (System.IO.File.Exists(finalPath))
                    {
                        System.IO.File.Delete(finalPath);
                    }

                    try
                    {
                        using (System.IO.StreamWriter stream = System.IO.File.CreateText(finalPath))
                        {
                            stream.WriteLine(data);
                        }
                    } catch (Exception)
                    {

                    }

                }
            }
        }

        private string GetPrayerTimeDataString()
        {
            string result = null;
            bool isFile = System.IO.File.Exists(DataFilePath);
            bool isDirectory = System.IO.Directory.Exists(DataFilePath);

            if (isFile || isDirectory)
            {
                string finalPath = null;

                if (isFile)
                {
                    //Get parent directory
                    string directory = System.IO.Path.GetDirectoryName(DataFilePath);
                    if (directory != null && !directory.Equals(String.Empty))
                    {
                        finalPath = System.IO.Path.Combine(directory, "prayertimes.json");
                    }
                }
                else if (isDirectory)
                {
                    finalPath = System.IO.Path.Combine(DataFilePath, "prayertimes.json");
                }

                if (finalPath != null || !finalPath.Equals(String.Empty))
                {
                    if (System.IO.File.Exists(finalPath))
                    {
                        try
                        {
                            using (System.IO.StreamReader stream = System.IO.File.OpenText(finalPath))
                            {
                                result = stream.ReadToEnd();
                            }
                        } catch (Exception)
                        {

                        }
                    }

                }
            }
            return result;
        }

        private void SetPrayerTimesFromRequest(Request request)
        {
            DateTime now = DateTime.Now;
            foreach (Data data in request.data)
            {
                if (data.date.gregorian.day.TrimStart('0').Equals(now.Day.ToString()) 
                    && data.date.gregorian.month.number.TrimStart('0').Equals(CurrentSettings.Month) 
                    && data.date.gregorian.year.TrimStart('0').Equals(CurrentSettings.Year))
                {
                    TodayPrayerTimes = data.timings;
                    TodayPrayerTimes.Fajr = TodayPrayerTimes.Fajr.Split('(')[0].Trim();
                    TodayPrayerTimes.Dhuhr = TodayPrayerTimes.Dhuhr.Split('(')[0].Trim();
                    TodayPrayerTimes.Imsak = TodayPrayerTimes.Imsak.Split('(')[0].Trim();
                    TodayPrayerTimes.Asr = TodayPrayerTimes.Asr.Split('(')[0].Trim();
                    TodayPrayerTimes.Maghrib = TodayPrayerTimes.Maghrib.Split('(')[0].Trim();
                    TodayPrayerTimes.Isha = TodayPrayerTimes.Isha.Split('(')[0].Trim();
                    TodayPrayerTimes.Midnight = TodayPrayerTimes.Midnight.Split('(')[0].Trim();
                    TodayPrayerTimes.Sunrise = TodayPrayerTimes.Sunrise.Split('(')[0].Trim();
                    TodayPrayerTimes.Sunset = TodayPrayerTimes.Sunset.Split('(')[0].Trim();

                    if (CurrentSettings._12HourMode)
                    {
                        ConvertPrayerTimesTo12Hour();
                    }
                    SetHijriDate(data);

                    return;
                }
            }
        }

        private void ConvertPrayerTimesTo12Hour()
        {
            TodayPrayerTimes.Fajr = ConvertStringTo12HourTime(TodayPrayerTimes.Fajr);
            TodayPrayerTimes.Dhuhr = ConvertStringTo12HourTime(TodayPrayerTimes.Dhuhr);
            TodayPrayerTimes.Imsak = ConvertStringTo12HourTime(TodayPrayerTimes.Imsak);
            TodayPrayerTimes.Asr = ConvertStringTo12HourTime(TodayPrayerTimes.Asr);
            TodayPrayerTimes.Maghrib = ConvertStringTo12HourTime(TodayPrayerTimes.Maghrib);
            TodayPrayerTimes.Isha = ConvertStringTo12HourTime(TodayPrayerTimes.Isha);
            TodayPrayerTimes.Midnight = ConvertStringTo12HourTime(TodayPrayerTimes.Midnight);
            TodayPrayerTimes.Sunrise = ConvertStringTo12HourTime(TodayPrayerTimes.Sunrise);
            TodayPrayerTimes.Sunset = ConvertStringTo12HourTime(TodayPrayerTimes.Sunset);
        }

        private string ConvertStringTo12HourTime(string time)
        {
            if (time[0] == '0')
            {
                return time.Substring(1);
            } else if ((time[0] == '1' && time[1] > '2') || time[0] == '2')
            {
                int hour = Int32.Parse(time.Substring(0, 2));
                hour %= 12;
                return hour.ToString() + time.Substring(2);
            } 
            return time;
        }

        private void SetHijriDate(Data requestData)
        {
            string monthName;
            switch (Int32.Parse(requestData.date.hijri.month.number))
            {
                default:
                case 1: monthName = "Muharram"; break;
                case 2: monthName = "Safar"; break;
                case 3: monthName = "Rabi Al-Awwal"; break;
                case 4: monthName = "Rabi Al-Akhir"; break;
                case 5: monthName = "Jumada Al-Awwal"; break;
                case 6: monthName = "Jumada Al-Akhirah"; break;
                case 7: monthName = "Rajab"; break;
                case 8: monthName = "Shaban"; break;
                case 9: monthName = "Ramadan"; break;
                case 10: monthName = "Shawwal"; break;
                case 11: monthName = "Dhul Qadah"; break;
                case 12: monthName = "Dhul Hijjah"; break;
            }
            HijriDate = monthName + " " + requestData.date.hijri.day + " " + requestData.date.hijri.year;
        }

        private bool IsStoredPrayerTimesSameAsSettings(Request request)
        {
            Meta metadata = request.data[0].meta;
            return (metadata.latitude == CurrentSettings.Latitude && metadata.longitude == CurrentSettings.Longitude);
        }

        public string GetFajrTime()
        {
            return TodayPrayerTimes?.Fajr;
        }

        public string GetSunriseTime()
        {
            return TodayPrayerTimes?.Sunrise;
        }

        public string GetDhuhrTime()
        {
            return TodayPrayerTimes?.Dhuhr;
        }

        public string GetAsrTime()
        {
            return TodayPrayerTimes?.Asr;
        }

        public string GetMaghribTime()
        {
            return TodayPrayerTimes?.Maghrib;
        }

        public string GetSunsetTime()
        {
            return TodayPrayerTimes?.Sunset;
        }

        public string GetIshaTime()
        {
            return TodayPrayerTimes?.Isha;
        }

        public string GetImsakTime()
        {
            return TodayPrayerTimes?.Imsak;
        }

        public string GetMidnightTime()
        {
            return TodayPrayerTimes?.Midnight;
        }

        public string GetHijriDate()
        {
            return HijriDate;
        }
    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            string path = Rainmeter.API.GetSettingsFile();
            Rainmeter.API api = (Rainmeter.API)rm;
            string latitude = api.ReadString("Latitude", "21.3891", false);
            string longitude = api.ReadString("Longitude", "39.8579", false);
            int method = api.ReadInt("Method", 4);
            int school = api.ReadInt("School", 0);
            int latitudeAdjustmentMethod = api.ReadInt("LatitudeAdjustmentMethod", 1);
            int hijriAdjustment = api.ReadInt("HijriAdjustment", 0);
            int midnightMode = api.ReadInt("MidnightMode", 0);
            bool _12hourmode = api.ReadInt("12HourClockMode", 24) == 12;
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure(path, latitude, longitude, method, school, latitudeAdjustmentMethod, hijriAdjustment, midnightMode, _12hourmode)));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)data;
            Rainmeter.API api = (Rainmeter.API)rm;
            string latitude = api.ReadString("Latitude", "21.3891", false);
            string longitude = api.ReadString("Longitude", "39.8579", false);
            int method = api.ReadInt("Method", 4);
            int school = api.ReadInt("School", 0);
            int latitudeAdjustmentMethod = api.ReadInt("LatitudeAdjustmentMethod", 1);
            int hijriAdjustment = api.ReadInt("HijriAdjustment", 0);
            int midnightMode = api.ReadInt("MidnightMode", 0);
            bool _12hourmode = api.ReadInt("12HourClockMode", 24) == 12;
            measure.Reload(latitude, longitude, method, school, latitudeAdjustmentMethod, hijriAdjustment, midnightMode, _12hourmode);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;
            measure.Update();

            return 0.0;
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni("Prayer time plugin");

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetFajrTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetFajrTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetSunriseTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetSunriseTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetDhuhrTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetDhuhrTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetAsrTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetAsrTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetMaghribTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetMaghribTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetIshaTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetIshaTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetMidnightTime(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetMidnightTime());

            return measure.buffer;
        }

        [DllExport]
        public static IntPtr GetHijriDate(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            measure.buffer = Marshal.StringToHGlobalUni(measure.GetHijriDate());

            return measure.buffer;
        }
    }
}

