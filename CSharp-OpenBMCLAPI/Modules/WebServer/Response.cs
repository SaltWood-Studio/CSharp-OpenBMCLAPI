using System.Text;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Response
    {
        public int StatusCode { get; set; } = 200;
        public Headers Header { get; set; } = new Headers();
        public Stream Stream { get; set; } = new MemoryStream();

        public async Task Call(Client client, Request request)
        {
            if (!Header.ContainsKey("Content-Length")) Header.Set("Content-Length", Stream.Length);
            Header.Set("X-Powered-By", "CSharp-SaltWood");
            string responseHeader = $"HTTP/1.1 {StatusCode} {GetStatusMsg(StatusCode)}\r\n{Header}\r\n\r\n";
            await client.Write(responseHeader.Encode());
            Stream.CopyTo(client.Stream);
            client.Stream.Flush();
            Stream.Close();
        }

        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public static readonly Dictionary<int, string> STATUS_CODES = new Dictionary<int, string>
        {
            { 100, "Continue" }, // 继续
            { 101, "Switching Protocols" }, // 切换协议
            { 102, "Processing" }, // 处理中
            { 103, "Early Hints" }, // 预加载资源
            { 200, "OK" }, // 请求成功
            { 201, "Created" }, // 成功创建资源
            { 202, "Accepted" }, // 收到请求
            { 203, "Non-Authoritative Information" }, // 服务器成功处理请求，但返回信息可能不是原始服务器上的有效集合，可能是本地或第三方拷贝
            { 204, "No Content" }, // 没有 body
            { 205, "Reset Content" }, // 重置文档
            { 206, "Partial Content" }, // 部分资源
            { 207, "Multi-Status" }, // 不知道选哪个状态码，所以干脆全都丢给你
            { 208, "Already Reported" }, // 在 DAV 里面使用 <dav:propstat> 响应元素以避免重复枚举多个绑定的内部成员到同一个集合
            { 226, "IM Used" }, // 服务器已经完成了对资源的GET请求，并且响应是对当前实例应用的一个或多个实例操作结果的表示
            { 300, "Multiple Choices" }, // 不知道你要哪个，干脆全部丢给你，让你自己选
            { 301, "Moved Pemanently" }, // 资源永久迁移啦
            { 302, "Found" }, // 暂时搬走了，去这里找；以后有啥更改，还是在老地方通知
            { 303, "See Other" }, // 用 GET 方法去这个地方找资源，别来我这儿
            { 304, "Not Modified" }, // 改都没改
            { 305, "Use Proxy" }, // 你还是开个代理吧
            { 306, "Unused" }, // 鬼知道有什么用
            { 307, "Temporary Redirect" }, // 跟 302 一样，但你用的什么方法原来是什么，现在也必须也用什么
            { 308, "308 Permanent Redirect" }, // 跟 301 一样，但你用的是方法原来是什么，现在也必须也用什么
            { 400, "Bad Request" }, // 你的锅，给我整不会了
            { 401, "Unauthorized" }, // 你谁啊？
            { 402, "Payment Required" }, // V 我 50
            { 403, "Forbidden" }, // 禁 止 访 问
            { 404, "Not Found" }, // 没 有
            { 405, "Method Not Allowed" }, // 方 法 不 对
            { 406, "Not Acceptable" }, // 当 web 服务器在执行服务端驱动型内容协商机制后，没有发现任何符合用户代理给定标准的内容时，就会发送此响应
            { 407, "Proxy Authentication Required" }, // 你代理谁啊？
            { 408, "Request Time-out" }, // 超 时 了
            { 409, "Conflict" }, // 公 交 车（划）冲 突 了
            { 410, "Gone" }, // 资 源 死 了 ， 删 了 吧
            { 411, "Length Required" }, // 你要多长，你不告诉我，我咋知道
            { 412, "Precondition Failed" }, // 不是不报，条件未满足
            { 413, "Payload Too Large" }, // 请 求 实 体 太 大
            { 414, "URI Too Long" }, // 链 接 太 长
            { 415, "Unsupported Media Type" }, // 请求数据的媒体格式不支持
            { 416, "Range Not Satisfiable" }, // 你范围太大了
            { 417, "Expectation Failed" }, // 对不起，我做不到（指 Expect）
            { 418, "I'm a teapot" }, // 我 是 茶 壶
            { 421, "Misdirected Request" }, // 请求被定向到无法生成响应的服务器
            { 422, "Unprocessable Entity" }, // 格式正确但语义错误
            { 423, "Locked" }, // 锁上了
            { 424, "Failed Dependency" }, // 多米诺骨牌倒了，上一个请求炸了，这个也不行
            { 425, "Too Early" }, // 你来的真早！当前服务器不愿意冒风险处理请求，请稍等几分钟
            { 426, "Upgrade Required" }, // 发 现 新 版 本
            { 428, "Precondition Required" }, // 我是有条件嘀~
            { 429, "Too Many Requests" }, // 太快了，停下来！
            { 431, "Request Header Fields Too Large" }, // 请求头太大了
            { 451, "Unavailable For Legal Reasons" }, // 法 律 封 锁
            { 500, "Internal Server Eror" }, // 给服务器整不会了
            { 501, "Not Implemented" }, // 我 不 会
            { 502, "Bad Gateway" }, // 网关炸了
            { 503, "Service Unavailable" }, // 伺 服 器 維 護 中 ， 將 結 束 應 用 程 式 。
            { 504, "Gateway Time-out" }, // 网关超时
            { 505, "HTTP Version not supported" }, // HTTP 版本不支持
            { 506, "Variant Also Negotiates" }, // 服务器配置文件炸了
            { 507, "Insufficient Storage" }, // 无法在资源上执行该方法
            { 508, "Loop Detected" }, // 死 循 环 辣
            { 510, "Not Extended" }, // 你得扩展扩展
            { 511, "Network Authentication Required" } // 登 录 校 园 网
        };

        public static string GetStatusMsg(int status)
        {
            return STATUS_CODES.ContainsKey(status) ? STATUS_CODES[status] : STATUS_CODES[status / 100 * 100];
        }

        public ValueTask SendFile(string filePath)
        {
            this.Stream = File.OpenRead(filePath);
            return ValueTask.CompletedTask;
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            return this.Stream.WriteAsync(buffer, offset, count);
        }

        public Task WriteAsync(byte[] buffer)
        {
            return this.Stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public Task WriteAsync(string data) => this.WriteAsync(Encoding.UTF8.GetBytes(data));

        public void ResetStreamPosition() => this.Stream.Position = 0;
    }
}
