using CSharpOpenBMCLAPI.Modules.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DataStatistician
    {
        private Task _updateTask;

        public DataStatistician()
        {
            for (int i = 0; i < this.Dashboard.Hours.Length; i++)
            {
                this.Dashboard.Hours[i]._hour = i;
            }
            for (int i = 0; i < this.Dashboard.Days.Length; i++)
            {
                this.Dashboard.Days[i]._day = i;
            }
            this._updateTask = Task.Run(() =>
            {
                FileAccessInfo fai = new()
                {
                    bytes = 0,
                    hits = 0
                };
                while (true)
                {
                    this.DownloadCount(fai);
                    Thread.Sleep(1000);
                }
            });
        }

        private Pair<long, int>[] qps = new Pair<long, int>[60];

        public Dictionary<long, int> Qps
        {
            get
            {
                Dictionary<long, int> pairs = new Dictionary<long, int>();
                lock (qps)
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
            lock (qps)
            {
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
            }
            lock (Dashboard)
            {
                Dashboard.Hours[DateTime.Now.Hour].bytes += fileAccessInfo.bytes;
                Dashboard.Hours[DateTime.Now.Hour].hits += fileAccessInfo.hits;
                Dashboard.Hours[DateTime.Now.Hour].last_bytes += fileAccessInfo.bytes;
                Dashboard.Hours[DateTime.Now.Hour].last_hits += fileAccessInfo.hits;
                Dashboard.Days[DateTime.Now.Day - 1].bytes += fileAccessInfo.bytes;
                Dashboard.Days[DateTime.Now.Day - 1].hits += fileAccessInfo.hits;
                Dashboard.Days[DateTime.Now.Day - 1].last_bytes += fileAccessInfo.bytes;
                Dashboard.Days[DateTime.Now.Day - 1].last_hits += fileAccessInfo.hits;
            }
        }

        private long startTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        public double Uptime => startTime;

        public long Memory
        {
            get
            {
                Process proc = Process.GetCurrentProcess();
                long b = proc.PrivateMemorySize64;
                return b;
            }
        }

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

        public DashboardInformation Dashboard { get; set; } = new();

        public int Connections
        {
            get
            {
                IPGlobalProperties properti = IPGlobalProperties.GetIPGlobalProperties();
                var tcps = properti.GetActiveTcpConnections().ToList();

                var list = tcps.Where(f => f.LocalEndPoint.Port == SharedData.Config.PORT);

                var iplist = list.GroupBy(f => f.RemoteEndPoint.Address);
                return iplist.Count();
            }
        }
    }
}
