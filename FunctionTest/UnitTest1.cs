using CSharpOpenBMCLAPI.Modules.WebServer;
using System.Diagnostics;
using System.Text;

namespace FunctionTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            byte[] data = Encoding.UTF8.GetBytes("GET /1024 HTTP/1.1\r\nHost: localhost\r\n\r\n");
            var resp = new Request(null!, data);
            var str = resp.ToString();
            Assert.AreEqual(str, """
                GET /1024 HTTP/1.1

                Headers:
                    Host: localhost

                Data:


                """);
        }
    }
}