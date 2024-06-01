using CSharpOpenBMCLAPI.Modules.Storage;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DataStatistician
    {
        private Task _updateTask;
        private bool _started;

        public DataStatistician()
        {
            this._started = false;
            this._updateTask = Task.Run(() =>
            {
                FileAccessInfo fai = new()
                {
                    bytes = 0,
                    hits = 0
                };
                while (true)
                {
                    if (this._started)
                    {
                        this.DownloadCount(fai);
                        Thread.Sleep(1000);
                    }
                }
            });
        }

        private Pair<long, int>[] qps = new Pair<long, int>[60];

        [Browsable(false)]
        public Dictionary<long, int> Qps
        {
            get
            {
                Dictionary<long, int> pairs = new Dictionary<long, int>();
                lock (this)
                {
                    foreach (var pair in qps)
                    {
                        pairs[pair.Key] = pair.Value;
                    }
                }
                return pairs;
            }
        }

        public void DownloadCount(FileAccessInfo fileAccessInfo)
        {
            lock (this)
            {
                this._started = true;
                Pair<long, int> last = qps[0];
                if (last.Key == 0)
                {
                    last.Key = DateTimeOffset.Now.ToUnixTimeSeconds() / 5;
                }
                else if (last.Key != DateTimeOffset.Now.ToUnixTimeSeconds() / 5)
                {
                    qps[0..^1].CopyTo(qps, 1);
                    last.Value = 0;
                    last.Key = DateTimeOffset.Now.ToUnixTimeSeconds() / 5;
                }
                last.Value += (int)fileAccessInfo.hits;
                qps[0] = last;
                HourAccessData[DateTime.Now.Hour].bytes += fileAccessInfo.bytes;
                HourAccessData[DateTime.Now.Hour].hits += fileAccessInfo.hits;
                DayAccessData[DateTime.Now.Day - 1].bytes += fileAccessInfo.bytes;
                DayAccessData[DateTime.Now.Day - 1].hits += fileAccessInfo.hits;
                MonthAccessData[DateTime.Now.Month - 1].bytes += fileAccessInfo.bytes;
                MonthAccessData[DateTime.Now.Month - 1].hits += fileAccessInfo.hits;
            }
        }

        [Browsable(false)]
        private long startTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        [Browsable(false)]
        public double Uptime => startTime;

        [Browsable(false)]
        public long Memory
        {
            get
            {
                Process proc = Process.GetCurrentProcess();
                long b = proc.PrivateMemorySize64;
                return b;
            }
        }

        [Browsable(false)]
        public double Cpu
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
                var cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                cpuCounter.NextValue();
                Thread.Sleep(40);
                return cpuCounter.NextValue();
            }
        }

        public AccessData[] HourAccessData { get; set; } = new AccessData[24];
        public AccessData[] DayAccessData { get; set; } = new AccessData[31];
        public AccessData[] MonthAccessData { get; set; } = new AccessData[12];

        [Browsable(false)]
        public int Connections
        {
            get
            {
                IPGlobalProperties properti = IPGlobalProperties.GetIPGlobalProperties();
                var tcps = properti.GetActiveTcpConnections();

                var list = tcps.Where(f => f.LocalEndPoint.Port == ClusterRequiredData.Config.PORT);

                var iplist = list.GroupBy(f => f.RemoteEndPoint.Address);
                return iplist.Count();
            }
        }
    }
}
