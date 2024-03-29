using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public struct DayAccessData
    {
        public long _day;
        public long hits;
        public long bytes;
        public long cache_hits;
        public long cache_bytes;
        public long last_hits;
        public long last_bytes;
        public long failed;
    }

    public struct HourAccessData
    {
        public long _hour;
        public long hits;
        public long bytes;
        public long cache_hits;
        public long cache_bytes;
        public long last_hits;
        public long last_bytes;
        public long failed;
    }


    public class DashboardInformation
    {
        [JsonProperty("days")]
        public DayAccessData[] Days = new DayAccessData[31];
        [JsonProperty("hourly")]
        public HourAccessData[] Hours = new HourAccessData[24];
    }
}