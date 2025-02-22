﻿using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.RpcPerformance
{
    [Command(Name = "Transaction Client", Description = "Monitor contract transaction testing client.")]
    [HelpOption("-?")]
    class Program
    {
        #region Parameter Option

        [Option("-tc|--thread.count", Description =
            "Thread count to execute transactions. Default value is 4")]
        private int ThreadCount { get; } = ConfigInfoHelper.Config.GroupCount;

        [Option("-tg|--transaction.group", Description =
            "Transaction count to execute of each round or one round. Default value is 10.")]
        private int TransactionGroup { get; } = ConfigInfoHelper.Config.TransactionCount;

        [Option("-ru|--rpc.url", Description = "Rpc service url of node. It's required parameter.")]
        private string RpcUrl { get; } = ConfigInfoHelper.Config.ServiceUrl;

        [Option("-em|--execute.mode", Description =
            "Transaction execution mode include: \n0. Not set \n1. Normal mode \n2. Continuous Tx mode \n3. Batch mode \n4. Continuous Txs mode")]
        private int ExecuteMode { get; } = ConfigInfoHelper.Config.ExecuteMode;

        [Option("-lt|--limit.transaction", Description =
            "Enable limit transaction, if transaction pool with enough transaction, request process be would wait.")]
        private string LimitTransactionString { get; } = "true";

        private bool LimitTransaction => LimitTransactionString.ToLower().Trim() == "true";

        #endregion

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static int Main(string[] args)
        {
            if (args.Length != 3) return CommandLineApplication.Execute<Program>(args);

            var tc = args[0];
            var tg = args[1];
            var ru = args[2];
            args = new[] {"-tc", tc, "-tg", tg, "-ru", ru, "-em", "0"};

            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute(CommandLineApplication app)
        {
            if (ThreadCount == 0 || TransactionGroup == 0 || RpcUrl == null)
            {
                app.ShowHelp();
                return;
            }

            var transactionType = ConfigInfoHelper.Config.ReadOnlyTransaction;
            var performance = transactionType
                ? (IPerformanceCategory) new ReadOnlyCategory(ThreadCount, TransactionGroup, RpcUrl,
                    limitTransaction: LimitTransaction)
                : new ExecutionCategory(ThreadCount, TransactionGroup, RpcUrl, limitTransaction: LimitTransaction);

            //Init Logger
            var logName = "RpcTh_" + performance.ThreadCount + "_Tx_" + performance.ExeTimes + "_" +
                          DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            //Execute transaction command
            try
            {
                if (ExecuteMode == 0) //检测链交易和出块结果
                {
                    Logger.Info("Check node transaction status information");
                    var apiHelper = new WebApiHelper(performance.BaseUrl);
                    var nodeSummary = new ExecutionSummary(apiHelper, true);
                    nodeSummary.ContinuousCheckTransactionPerformance();
                    return;
                }

                performance.InitExecCommand(1000 + ThreadCount);
                performance.DeployContracts();
                performance.InitializeContracts();

                ExecuteTransactionPerformanceTask(performance, ExecuteMode);
            }
            catch (Exception e)
            {
                var message = $"Message: {e.Message}\r\nSource: {e.Source}\r\nStackTrace: {e.StackTrace}";
                Logger.Error(message);
            }

            //Result summary
            var set = new CategoryInfoSet(performance.ApiHelper.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            var xmlFile = set.SaveTestResultXml(performance.ThreadCount, performance.ExeTimes);
            Logger.Info("Log file: {0}", dir);
            Logger.Info("Xml file: {0}", xmlFile);
            Logger.Info("Complete performance testing.");
        }

        private static void ExecuteTransactionPerformanceTask(IPerformanceCategory performance, int execMode = -1)
        {
            if (execMode == -1)
            {
                Logger.Info("Select execution type:");
                "1. Normal mode".WriteSuccessLine();
                "2. Continue Tx mode".WriteSuccessLine();
                "3. Batch mode".WriteSuccessLine();
                "4. Continue Txs mode".WriteSuccessLine();
                Console.Write("Input selection: ");

                var runType = Console.ReadLine();
                var check = int.TryParse(runType, out execMode);
                if (!check)
                {
                    Logger.Info("Wrong input, please input again.");
                    ExecuteTransactionPerformanceTask(performance);
                }
            }

            var tm = (TestMode) execMode;
            var conflict = ConfigInfoHelper.Config.Conflict;
            switch (tm)
            {
                case TestMode.CommonTx:
                    Logger.Info($"Run with tx mode: {tm.ToString()}.");
                    performance.ExecuteOneRoundTransactionTask();
                    break;
                case TestMode.ContinuousTx:
                    Logger.Info($"Run with continuous tx mode: {tm.ToString()}.");
                    performance.ExecuteContinuousRoundsTransactionsTask();
                    break;
                case TestMode.BatchTxs:
                    Logger.Info($"Run with txs mode: {tm.ToString()}.");
                    performance.ExecuteOneRoundTransactionsTask();
                    break;
                case TestMode.ContinuousTxs:
                    Logger.Info($"Run with continuous txs mode: {tm.ToString()}.");
                    performance.ExecuteContinuousRoundsTransactionsTask(true, conflict);
                    break;
                case TestMode.NotSet:
                    break;
                default:
                    Logger.Info("Wrong input, please input again.");
                    ExecuteTransactionPerformanceTask(performance);
                    break;
            }

            performance.PrintContractInfo();
        }
    }
}