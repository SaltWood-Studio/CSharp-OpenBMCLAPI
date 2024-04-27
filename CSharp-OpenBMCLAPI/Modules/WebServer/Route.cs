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
        public required Regex MatchRegex;
        public List<Func<string, bool>> ConditionExpressions = new();
        public Action<HttpContext, Cluster, Match>? Handler;
        public string Methods
        {
            get => string.Join(' ', this._allowedMethods);
            set
            {
                this._allowedMethods.Clear();
                foreach (string method in value.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    this._allowedMethods.Add(method);
                }
            }
        }

        private List<string> _allowedMethods = new()
        {
            "GET"
        };

        public Route()
        {

        }
    }
}
