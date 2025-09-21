using Newtonsoft.Json;

namespace CSharpOpenBMCLAPI.Modules
{
    public readonly struct Configuration
    {
        [JsonProperty("sync")]
        public Sync Sync { get; init; }
    }

    public readonly struct Sync
    {
        [JsonProperty("source")]
        public string Source { get; init; }

        [JsonProperty("concurrency")]
        public int Concurrency { get; init; }
    }
}