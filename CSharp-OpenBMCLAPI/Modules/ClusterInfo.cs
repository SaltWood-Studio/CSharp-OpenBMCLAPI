using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    public struct ClusterInfo
    {
        public string ClusterID, ClusterSecret;

        public ClusterInfo(string id, string secret)
        {
            this.ClusterID = id;
            this.ClusterSecret = secret;
        }
    }
}
