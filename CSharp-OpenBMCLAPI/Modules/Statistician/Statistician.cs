namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public struct DayStatistician
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

    public struct HourStatistician
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
}