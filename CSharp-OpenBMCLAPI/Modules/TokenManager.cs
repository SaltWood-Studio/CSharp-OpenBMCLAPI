using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CSharpOpenBMCLAPI.Modules
{
    public class TokenManager
    {
        private readonly string CLUSTER_ID, CLUSTER_SECRET;
        private Task? _updateTask;

        private Token token;
        private object _writeLocker = new object();

        public Token Token { get { lock (_writeLocker) return token; } }

        public TokenManager(ClusterInfo info)
        {
            this.CLUSTER_ID = info.ClusterID;
            this.CLUSTER_SECRET = info.ClusterSecret;
        }

        public async Task<Token> FetchToken()
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

            // 锁定 token，以免其他线程同时进行读取出现问题
            lock (this._writeLocker)
            {
                this.token = JsonConvert.DeserializeObject<Token>(tokenJson).ThrowIfNull();

                this._updateTask = Task.Run(() =>
                {
                    Thread.Sleep(this.token.ttl - SharedData.Config.refreshTokenTime);
                    RefreshToken();
                });
            }

            SharedData.Logger.LogInfo($"Token 刷新成功：有效时长<{TimeSpan.FromMilliseconds(this.token.ttl).ToString(@"dd\:hh\:mm\:ss")}>");
            return token;
        }

        public void RefreshToken()
        {
            FetchToken().Wait();
        }
    }
}
