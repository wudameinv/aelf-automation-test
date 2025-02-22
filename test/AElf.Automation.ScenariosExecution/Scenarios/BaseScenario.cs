using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class BaseScenario
    {
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        protected List<string> AllTesters { get; set; }
        protected List<Node> BpNodes { get; set; }
        protected List<Node> FullNodes { get; set; }
        protected static ContractServices Services { get; set; }

        protected void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true,
            int sleepSeconds = 0)
        {
            while (true)
            {
                try
                {
                    if (actions == null)
                        throw new ArgumentException("Action methods is null.");
                    ExecuteStandaloneTask(actions, sleepSeconds);
                }
                catch (Exception e)
                {
                    Logger.Error($"ExecuteContinuousTasks got exception: {e.Message}");
                    if (interrupted)
                        break;
                }
            }
        }

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0)
        {
            try
            {
                foreach (var action in actions)
                {
                    action.Invoke();
                }

                if (sleepSeconds != 0)
                    Thread.Sleep(1000 * sleepSeconds);
            }
            catch (Exception e)
            {
                Logger.Error($"ExecuteStandaloneTask got exception: {e.Message}");
            }
        }

        public void CheckNodeTransactionAction()
        {
            var chain = new ChainSummary(Services.ApiHelper.GetApiUrl());
            chain.ContinuousCheckChainStatus();
        }

        public void CheckNodeStatusAction()
        {
            while (true)
            {
                CheckNodeHeightStatus();
                Thread.Sleep(30 * 1000);
            }
        }

        private void CheckNodeHeightStatus()
        {
            long height = 1;
            var checkTimes = 0;
            while (true)
            {
                if (checkTimes == 120)
                    break;

                var newHeight = Services.ApiHelper.ApiService.GetBlockHeight().Result;
                if (newHeight == height)
                {
                    checkTimes++;
                    if (checkTimes % 10 == 0)
                        Logger.Warn($"Node height not changed {checkTimes / 2} seconds.");
                    Thread.Sleep(500);
                }
                else
                {
                    height = newHeight;
                    checkTimes = 0;
                }
            }

            Assert.IsTrue(false, $"Node height not changed 1 minutes later.");
        }

        protected void InitializeScenario()
        {
            var envCheck = EnvCheck.GetDefaultEnvCheck();
            AllTesters = envCheck.GenerateOrGetTestUsers();
            if (Services == null)
                Services = envCheck.GetContractServices();

            var configInfo = ConfigInfoHelper.Config;
            BpNodes = configInfo.BpNodes;
            FullNodes = configInfo.FullNodes;
        }

        protected static int GenerateRandomNumber(int min, int max)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            return random.Next(min, max);
        }
    }
}