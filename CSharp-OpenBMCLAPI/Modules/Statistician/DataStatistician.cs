using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Statistician
{
    public class DataStatistician
    {
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

        public void DownloadCount()
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
                last.Value += 1;
                qps[0] = last;
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
                try
                {
                    CPUHelper helper = new CPUHelper();
                    return helper.GetCPUUsage().Average();
                }
                catch
                {
                    return 0;
                }
            }
        }

        public DashboardInformation Dashboard { get; set; } = new();

        public int Connections { get; set; } = 0; // TODO: Connections
    }

    public class CPUHelper
    {
        // 用于获得CPU信息
        PerformanceCounter[] counters;

        public CPUHelper()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new NotSupportedException();
            counters = new PerformanceCounter[Environment.ProcessorCount];
            for (int i = 0; i < counters.Length; i++)
            {
                counters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                counters[i].NextValue();
            }
        }

        public double[] GetCPUUsage()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new NotSupportedException();
            double[] info = new double[counters.Length];
            for (int i = 0; i < counters.Length; i++)
                info[i] = counters[i].NextValue();

            return info;
        }
    }

}
