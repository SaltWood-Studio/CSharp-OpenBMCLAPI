using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules
{
    public struct Configuration
    {
        [JsonProperty("sync")]
        public Sync Sync { get; set; }
    }

    public struct Sync
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("concurrency")]
        public int Concurrency { get; set; }
    }
}