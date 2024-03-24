using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DashboardInformation
    {
        [JsonProperty("days")]
        public DayStatistician Days { get; set; }
        [JsonProperty("hourly")]
        public HourStatistician Hours { get; set; }
    }
}