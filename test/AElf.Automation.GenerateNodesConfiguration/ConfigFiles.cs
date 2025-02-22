using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.GenerateNodesConfiguration
{
    public class GenerateInformation
    {
        private string _minerInfo;
        private List<string> _bootNodes;

        public GenerateInformation()
        {
            _bootNodes = GetAllBootNodes();
        }

        public string GenerateBootNodeInfo(NodeOption node)
        {
            var array = _bootNodes.FindAll(o => o != $"\"{node.IpAddress}:{node.NetPort}\"");
            return string.Join(",", array);
        }

        public string GenerateMinerInfo()
        {
            if(_minerInfo != null) return _minerInfo;
            var bps = ConfigInfoHelper.Config.BpNodes;
            var pubKeys = bps.Select(o => $"\"{o.PublicKey}\"").ToList();
            _minerInfo = string.Join(",", pubKeys);

            return _minerInfo;
        }

        private List<string> GetAllBootNodes()
        {
            var bpNodes = ConfigInfoHelper.Config.BpNodes;
            var fullNodes = ConfigInfoHelper.Config.FullNodes;
            var nodes = bpNodes.Concat(fullNodes);

            return nodes.Select(node => $"\"{node.IpAddress}:{node.NetPort}\"").ToList();
        }
    }
    
    public class ConfigFiles
    {
        private readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly string _templateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        private const string LogFile = "log4net.config";
        private const string MainNetFile = "appsettings.MainChain.MainNet.json";
        private const string SettingFile = "appsettings.json";

        private readonly NodeOption _node;

        public ConfigFiles(NodeOption node)
        {
            _node = node;
        }

        public void GenerateBasicConfigFile()
        {
            var desPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results", _node.Name);
            
            //copy log setting
            var logFile = Path.Combine(_templateFolder, LogFile);
            CommonHelper.CopyFiles(logFile, desPath);
            Logger.WriteInfo($"{LogFile} generate success.");
            
            //copy main chain net file
            var mainFile = Path.Combine(_templateFolder, MainNetFile);
            CommonHelper.CopyFiles(mainFile, desPath);
            Logger.WriteInfo($"{MainNetFile} generate success.");
        }

        public void GenerateSettingFile(GenerateInformation info)
        {
            var content = ReadFiles(SettingFile);
            
            //update db number
            content = content.Replace("[DB_NO]", _node.DbNo.ToString());
            
            //update account
            content = content.Replace("[ACCOUNT]", _node.Account);
            
            //update api port
            content = content.Replace("[API_PORT]", _node.ApiPort.ToString());
            
            //update net port
            content = content.Replace("[NET_PORT]", _node.NetPort.ToString());
            
            //update miner and boot nodes
            var minerInfo = info.GenerateMinerInfo();            
            content = content.Replace("[MINERLIST]", minerInfo);

            var bootInfo = info.GenerateBootNodeInfo(_node);
            content = content.Replace("[BOOT_NODE]", bootInfo);
            
            //save setting file
            SaveSettingFiles(content);
        }
        
        private void SaveSettingFiles(string content)
        {
            var settingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results", _node.Name, SettingFile);
            File.WriteAllText(settingPath, content, Encoding.UTF8);
            Logger.WriteInfo($"{SettingFile} generate success.");
        }
        
        private string ReadFiles(string fileName)
        {
            var path = Path.Combine(_templateFolder, fileName);
            return File.ReadAllText(path);
        }
    }
}