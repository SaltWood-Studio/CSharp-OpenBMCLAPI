using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class WebMapper
    {
        protected ServeType serveType;
        protected WebApplication? aspWeb;
        protected SimpleWebServer? simpleWeb;

        public WebMapper(ServeType type)
        {
            this.serveType = type;
        }

        public void Map(string regexString)
        {
            Regex regex = new Regex(regexString);
        }
    }
}
