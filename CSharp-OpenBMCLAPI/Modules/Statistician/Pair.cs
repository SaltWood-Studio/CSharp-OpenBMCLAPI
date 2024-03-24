using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public struct Pair<T1, T2>
    {
        [JsonProperty("key")]
        public T1 Key { get; set; }
        [JsonProperty("value")]
        public T2 Value { get; set; }
    }
}
