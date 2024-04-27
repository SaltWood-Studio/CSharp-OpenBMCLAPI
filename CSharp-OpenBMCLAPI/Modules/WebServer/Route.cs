using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public struct Route
    {
        public required Regex matchRegex;
        public List<Func<string, bool>> conditionExpressions = new();
        public Action<HttpContext, Cluster, Match>? handler;

        public Route()
        {

        }
    }
}
