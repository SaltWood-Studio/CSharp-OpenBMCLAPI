using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    internal class TokenManager
    {
        private readonly string CLUSTER_ID, CLUSTER_SECRET;

        public TokenManager(ClusterInfo info)
        {
            this.CLUSTER_ID = info.ClusterID;
            this.CLUSTER_SECRET = info.ClusterSecret;
        }

        public async void FetchToken()
        {
            // 获取公用 client
            HttpClient client = HttpRequest.client;

            // 请求 challenge
            var resp = await client.GetAsync($"openbmclapi-agent/challenge?clusterId={CLUSTER_ID}");
            resp.EnsureSuccessStatusCode();
            string challengeJson = await resp.Content.ReadAsStringAsync();
            Challenge challenge = JsonConvert.DeserializeObject<Challenge>(challengeJson).ThrowIfNull();

            // 生成 signature
            var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(CLUSTER_SECRET)).ComputeHash(Encoding.UTF8.GetBytes(challenge.challenge))).ToLower();

            // 构建请求体
            Dictionary<string, string> requestBody = new()
            {
                ["clusterId"] = CLUSTER_ID,
                ["challenge"] = challenge.challenge,
                ["signature"] = signature
            };
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(requestBody), new MediaTypeHeaderValue("application/json"));

            resp = await client.PostAsync($"openbmclapi-agent/token", httpContent);
            string tokenJson = await resp.Content.ReadAsStringAsync();
            Token token = JsonConvert.DeserializeObject<Token>(tokenJson);

            return returns;
        }
    }
}
