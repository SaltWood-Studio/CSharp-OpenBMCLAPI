using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DashboardInformation
    {
        [JsonProperty("days")]
        public DayStatistician[] Days =
        {
            new DayStatistician
            {
                _day = 1,
                bytes = 1,
                cache_bytes = 1,
                cache_hits = 1,
                failed = 1,
                hits = 1,
                last_bytes = 1,
                last_hits = 1
            }
        };
        [JsonProperty("hourly")]
        public HourStatistician[] Hours =
        {
            new HourStatistician
            {
                _hour = 1,
                bytes = 1,
                cache_bytes = 1,
                cache_hits = 1,
                failed = 1,
                hits = 1,
                last_bytes = 1,
                last_hits = 1
            }
        };
    }
}