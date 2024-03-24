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
        public Pair<long, int>[] Qps = new Pair<long, int>[60];

        public void DownloadCount()
        {
            lock (this)
            {
                Pair<long, int> last = Qps[^1];
                if (last.Key == 0)
                {
                    last.Key = DateTime.Now.Ticks;
                }
                else if (last.Key != DateTime.Now.Ticks)
                {
                    Qps[1..^1].CopyTo(Qps, 0);
                    last.Key = DateTime.Now.Ticks;
                }
                last.Value += 1;
            }
        }

        private long startTime = DateTime.Now.Ticks;

        public long Uptime => DateTime.Now.Ticks - startTime;

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
