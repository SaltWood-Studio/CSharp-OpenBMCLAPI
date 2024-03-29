using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DashboardInformation
    {
        [JsonProperty("days")]
        public DayStatistician[] Days = new DayStatistician[31];
        [JsonProperty("hourly")]
        public HourStatistician[] Hours = new HourStatistician[24];
    }
}