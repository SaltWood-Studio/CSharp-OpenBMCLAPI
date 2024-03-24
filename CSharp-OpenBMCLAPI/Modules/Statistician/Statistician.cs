namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public struct DayStatistician
    {
        public long day;
        public long hits;
        public long bytes;
        public long cache_hits;
        public long cache_bytes;
    }

    public struct HourStatistician
    {
        public long hour;
        public long hits;
        public long bytes;
        public long cache_hits;
        public long cache_bytes;
    }
}