using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Rainmeter;
using Newtonsoft.Json;

namespace PluginNowPlayingJellyfin
{
    internal struct Data
    {
        public string Album;
        public string Artist;
        public string Title;
        public int Number;
        public string Year;
        public string Cover;
        public string File;
        public int Duration;
        public int Position;
        public double Progress;
        public int State;

        internal void clear()
        {
            Album = null;
            Artist = null;
            Title = null;
            Number = 0;
            Year = null;
            Cover = null;
            File = null;
            Duration = 0;
            Position = 0;
            Progress = 0;
            State = 0;
        }
    }

    internal class ApiPlayState
    {
        public long PositionTicks { get; set; }
        public bool IsPaused { get; set; }
    }

    internal class ApiNowPlayingItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Artists { get; set; }
        public string Album { get; set; }
        public string Path { get; set; }
        public string ParentId { get; set; }
        public long RunTimeTicks { get; set; }
        public string ProductionYear { get; set; }
        public int IndexNumber { get; set; }
    }

    internal class ApiSession
    {
        public string UserName { get; set; }
        public ApiPlayState PlayState { get; set; }
        public ApiNowPlayingItem NowPlayingItem { get; set; }
    }

    internal class Measure
    {
        internal enum MeasureType
        {
            Album,
            Artist,
            Title,
            Number,
            Year,
            Cover,
            File,
            Duration,
            Position,
            Progress,
            State
        }

        internal API api;

        internal MeasureType Type = MeasureType.Title;

        internal virtual void Dispose()
        {
        }

        internal virtual void Reload(Rainmeter.API api)
        {
            this.api = api;

            string type = api.ReadString("PlayerType", "");
            switch (type.ToLowerInvariant())
            {
                case "album":
                    Type = MeasureType.Album;
                    break;
                case "artist":
                    Type = MeasureType.Artist;
                    break;
                case "title":
                    Type = MeasureType.Title;
                    break;
                case "number":
                    Type = MeasureType.Number;
                    break;
                case "year":
                    Type = MeasureType.Year;
                    break;
                case "cover":
                    Type = MeasureType.Cover;
                    break;
                case "file":
                    Type = MeasureType.File;
                    break;
                case "duration":
                    Type = MeasureType.Duration;
                    break;
                case "position":
                    Type = MeasureType.Position;
                    break;
                case "progress":
                    Type = MeasureType.Progress;
                    break;
                case "state":
                    Type = MeasureType.State;
                    break;
                default:
                    api.Log(API.LogType.Error, "NowPlayingJellyfin: Type=" + type + " not valid");
                    break;
            }
        }

        internal virtual double Update()
        {
            return 0.0;
        }

        internal virtual string GetString()
        {
            return null;
        }
    }

    internal class ParentMeasure : Measure
    {
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();

        internal string Name;
        internal IntPtr Skin;

        internal string JellyfinToken;
        internal string JellyfinUsername;
        internal string JellyfinServer;
        internal int DisableLeadingZero;

        internal WebClient client = new WebClient();

        internal Data data = default(Data);

        internal ParentMeasure()
        {
            ParentMeasures.Add(this);
        }

        internal override void Dispose()
        {
            ParentMeasures.Remove(this);
        }

        internal override void Reload(Rainmeter.API api)
        {
            base.Reload(api);

            Name = api.GetMeasureName();
            Skin = api.GetSkin();

            JellyfinToken = api.ReadString("JellyfinToken", "");
            JellyfinUsername = api.ReadString("JellyfinUsername", "");
            JellyfinServer = api.ReadString("JellyfinServer", "http://localhost:8096");
            DisableLeadingZero = api.ReadInt("DisableLeadingZero", 0);

            if (string.IsNullOrEmpty(JellyfinToken))
            {
                api.Log(API.LogType.Error, "NowPlayingJellyfin: missing JellyfinToken");
            }
        }

        internal override double Update()
        {
            if (string.IsNullOrEmpty(JellyfinToken))
            {
                return GetValue(Type);
            }
            
            string url = JellyfinServer + "/Sessions?ApiKey=" + JellyfinToken;
            api.Log(API.LogType.Debug, "NowPlayingJellyfin: Query " + url);

            try
            {
                string response = client.DownloadString(url);
                List<ApiSession> sessions = JsonConvert.DeserializeObject<List<ApiSession>>(response);

                ApiSession session = sessions.Find(s => {
                    return s.NowPlayingItem != null
                        && s.NowPlayingItem.Type.Equals("Audio")
                        && (string.IsNullOrEmpty(JellyfinUsername) || s.UserName.Equals(JellyfinUsername));
                });

                if (session != null)
                {
                    ReadData(session);
                }
                else
                {
                    data.clear();
                }
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Error, "NowPlayingJellyfin: error querying : " + e.Message);
                data.clear();
            }

            return GetValue(Type);
        }

        internal override string GetString()
        {
            return GetString(Type);
        }

        internal string GetString(MeasureType type)
        {
            switch (type)
            {
                case MeasureType.Album:
                    return data.Album;
                case MeasureType.Artist:
                    return data.Artist;
                case MeasureType.Title:
                    return data.Title;
                case MeasureType.Number:
                    return data.Number.ToString();
                case MeasureType.Year:
                    return data.Year;
                case MeasureType.Cover:
                    return data.Cover;
                case MeasureType.Duration:
                    return FormatDuration(data.Duration);
                case MeasureType.Position:
                    return FormatDuration(data.Position);
                case MeasureType.Progress:
                    return data.Progress.ToString("F0");
                case MeasureType.State:
                    return data.State.ToString();
                default:
                    return null;
            }
        }

        internal double GetValue(MeasureType type)
        {
            switch (type)
            {
                case MeasureType.Number:
                    return data.Number;
                case MeasureType.Duration:
                    return Math.Floor(data.Duration / 1000.0);
                case MeasureType.Position:
                    return Math.Floor(data.Position / 1000.0);
                case MeasureType.Progress:
                    return data.Progress;
                case MeasureType.State:
                    return data.State;
                default:
                    return 0;
            }
        }

        internal void ReadData(ApiSession session)
        {
            try
            {
                data.Album = session.NowPlayingItem.Album;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Album : " + e.Message);
                data.Album = "";
            }
            try
            {
                data.Artist = session.NowPlayingItem.Artists[0];
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Artist : " + e.Message);
                data.Artist = "";
            }
            try
            {
                data.Title = session.NowPlayingItem.Name;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Title : " + e.Message);
                data.Title = "";
            }
            try
            {
                data.Number = session.NowPlayingItem.IndexNumber;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Number : " + e.Message);
                data.Number = 0;
            }
            try
            {
                data.Year = session.NowPlayingItem.ProductionYear;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Year : " + e.Message);
                data.Year = "";
            }
            try
            {
                data.Cover = JellyfinServer + "/Items/" + session.NowPlayingItem.ParentId + "/Images/Primary?fillHeight=600&fillWidth=600";
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Cover : " + e.Message);
                data.Cover = "";
            }
            try
            {
                data.File = session.NowPlayingItem.Path.Replace("\\\\", "\\");
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading File : " + e.Message);
                data.File = "";
            }
            try
            {
                data.Duration = (int) (session.NowPlayingItem.RunTimeTicks / 10000000.0);
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Duration : " + e.Message);
                data.Duration = 0;
            }
            try
            {
                data.State = session.PlayState.IsPaused ? 2 : 1;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading State : " + e.Message);
                data.State = 0;
            }
            try
            {
                data.Position = (int)(session.PlayState.PositionTicks / 10000000.0);
                data.Progress = (double) data.Position / data.Duration * 100.0;
            }
            catch (Exception e)
            {
                api.Log(API.LogType.Debug, "NowPlayingJellyfin: error reading Position : " + e.Message);
                data.Position = 0;
                data.Progress = 0;
            }
        }

        internal string FormatDuration(int duration)
        {
            int seconds = duration % 60;
            int minutes = (duration - seconds) / 60;
            return minutes.ToString().PadLeft(DisableLeadingZero == 1 ? 1 : 2, '0') + ":" + seconds.ToString().PadLeft(2, '0');
        }
    }

    internal class ChildMeasure : Measure
    {
        private ParentMeasure ParentMeasure = null;

        internal override void Reload(Rainmeter.API api)
        {
            base.Reload(api);

            string playerName = api.ReadString("PlayerName", "");
            IntPtr skin = api.GetSkin();

            ParentMeasure = null;
            foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(skin) && parentMeasure.Name.Equals(playerName))
                {
                    ParentMeasure = parentMeasure;
                }
            }

            if (ParentMeasure == null)
            {
                api.Log(API.LogType.Error, "NowPlayingJellyfin: PlayerName=" + playerName + " not valid");
            }
        }

        internal override double Update()
        {
            if (ParentMeasure != null)
            {
                return ParentMeasure.GetValue(Type);
            }

            return 0.0;
        }

        internal override string GetString()
        {
            if (ParentMeasure != null)
            {
                return ParentMeasure.GetString(Type);
            }

            return null;
        }
    }

    public static class Plugin
    {
        static IntPtr EMPTY_STR = Marshal.StringToHGlobalAuto("");

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            API api = new API(rm);

            string parent = api.ReadString("PlayerName", "");
            Measure measure;
            if (string.IsNullOrEmpty(parent))
            {
                measure = new ParentMeasure();
            }
            else
            {
                measure = new ChildMeasure();
            }

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm));
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            string value = measure.GetString();

            if (value != null)
            {
                return Marshal.StringToHGlobalAuto(value);
            }
            else
            {
                return EMPTY_STR;
            }
        }
    }
}
