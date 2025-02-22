using System;
using System.Collections.Generic;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Profit;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        //action
        public CommandInfo UserVote(string account, string candidate, int lockTime, long amount)
        {
            //check balance
            var beforeBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = Address.Parse(account),
                    Symbol = "ELF"
                }).Balance;

            ElectionService.SetAccount(account);
            var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePublicKey = ApiHelper.GetPublicKeyFromAddress(candidate),
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
            });
            var transactionResult = vote.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(account),
                Symbol = "ELF"
            }).Balance;

            beforeBalance.ShouldBe(afterBalance + amount, "user voted but user balance not correct.");

            return vote;
        }

        public List<string> UserVoteWithTxIds(string account, string candidate, int lockTime, int times)
        {
            ElectionService.SetAccount(account);
            var list = new List<string>();
            for (int i = 1; i <= times; i++)
            {
                var txId = ElectionService.ExecuteMethodWithTxId(ElectionMethod.Vote, new VoteMinerInput
                {
                    CandidatePublicKey = ApiHelper.GetPublicKeyFromAddress(candidate),
                    Amount = i,
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
                });

                list.Add(txId);
            }

            return list;
        }

        public CommandInfo ReleaseProfit(long period, int amount, string profitId)
        {
            var result =
                ProfitService.ExecuteMethodWithResult(ProfitMethod.ReleaseProfit, new ReleaseProfitInput
                {
                    Period = period,
                    Amount = amount,
                    ProfitId = HashHelper.HexStringToHash(profitId)
                });
            return result;
        }

        public CommandInfo Profit(string account, Hash profitId)
        {
            ProfitService.SetAccount(account);
            var result = ProfitService.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });

            return result;
        }

        #region TokenConverter Method

        // action
        public CommandInfo TokenConverterInitialize(string initAccount)
        {
            var ramConnector = new Connector
            {
                Symbol = "RAM",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var cpuConnector = new Connector
            {
                Symbol = "CPU",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var netConnector = new Connector
            {
                Symbol = "NET",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var stoConnector = new Connector
            {
                Symbol = "STO",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var elfConnector = new Connector
            {
                Symbol = "ELF",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                VirtualBalance = 100_0000,
                Weight = "0.5"
            };

            var result = TokenConverterService.ExecuteMethodWithResult(TokenConverterMethod.Initialize,
                new InitializeInput
                {
                    BaseTokenSymbol = "ELF",
                    ManagerAddress = Address.Parse(initAccount),
                    FeeReceiverAddress = Address.Parse(FeeReceiverService.ContractAddress),
                    FeeRate = "0.05",
                    TokenContractAddress = Address.Parse(TokenService.ContractAddress),
                    Connectors = {ramConnector, cpuConnector, netConnector, stoConnector, elfConnector}
                });

            return result;
        }

        //token action
        public CommandInfo TransferToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            return TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                To = Address.Parse(to),
                Memo = $"transfer {from}=>{to} with amount {amount}."
            });
        }

        public CommandInfo CreateToken(string issuer, string symbol, string tokenName)
        {
            TokenService.SetAccount(issuer);
            var create = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = Address.Parse(issuer),
                TokenName = tokenName,
                TotalSupply = 100_0000
            });
            return create;
        }

        public CommandInfo IssueToken(string issuer, string symbol, string toAddress)
        {
            TokenService.SetAccount(issuer);
            var issue = TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100_0000,
                Memo = "Issue",
                To = Address.Parse(toAddress)
            });

            return issue;
        }

        #endregion
    }
}