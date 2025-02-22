using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class Net_PeerTests
    {
        public static string Bp1Url = "http://192.168.197.13:8000";
        public string Bp2Url = "http://192.168.197.28:8000";
        public string Bp3Url = "http://192.168.197.33:8000";

        public string Full1Url = "http://192.168.199.205:8100";
        public string Full2Url = "http://192.168.199.205:8200";
        public string Full3Url = "http://192.168.199.205:8300";
        public string Full4Url = "http://192.168.199.205:8400";

        protected readonly ILogHelper _logger = LogHelper.GetLogHelper();
        protected IApiHelper CH { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            //Init Logger
            string logName = "NetPeersTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000")]
        public void GetPeers(string url)
        {
            var service = new WebApiService(url);
            var list = service.GetPeers().Result;
            _logger.Info($"Peer {url} information");
            foreach (var peer in list)
            {
                _logger.Info(JsonConvert.SerializeObject(peer));
            }
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000", "192.168.197.205:6810")]
        public void AddPeers(string url, params string[] addressArray)
        {
            var service = new WebApiService(url);
            if (addressArray == null) return;
            foreach (var address in addressArray)
            {
                var result = service.AddPeer(address).Result;
                _logger.Info($"Add peer {address} result: {result}");
            }
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000", "192.168.197.28:6800")]
        public void RemovePeers(string url, params string[] addressArray)
        {
            var service = new WebApiService(url);
            if (addressArray == null) return;
            foreach (var address in addressArray)
            {
                var result = service.RemovePeer(address).Result;
                _logger.Info($"Remove peer {address} result: {result}");
            }

            GetPeers(url);
        }
    }
}