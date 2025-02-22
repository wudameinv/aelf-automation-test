using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Profit;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class UserScenario : BaseScenario
    {
        public ElectionContract Election { get; }
        public ConsensusContract Consensus { get; }
        public ProfitContract Profit { get; }
        public TokenContract Token { get; }
        public List<string> Testers { get; }
        public Dictionary<ProfitType, Hash> ProfitItemIds { get; }

        private static List<string> _candidates;
        private static List<string> _candidatesExcludeMiners;

        public UserScenario()
        {
            InitializeScenario();
            Election = Services.ElectionService;
            Consensus = Services.ConsensusService;
            Profit = Services.ProfitService;
            Token = Services.TokenService;

            //Get Profit items
            Profit.GetProfitItemIds(Election.ContractAddress);
            ProfitItemIds = Profit.ProfitItemIds;

            Testers = AllTesters.GetRange(50, 30);
        }

        public void RunUserScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                UserVotesAction,
                TakeVotesProfitAction
            }, true, 30);
        }

        public void UserScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                UserVotesAction,
                TakeVotesProfitAction
            });
        }

        private void UserVotesAction()
        {
            GetCandidates(Election);
            GetCandidatesExcludeCurrentMiners();

            if (_candidates.Count < 2)
                return;

            var times = GenerateRandomNumber(3, 5);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count - 1);
                UserVote(Testers[id]);

                Thread.Sleep(10);
            }
        }

        private void TakeVotesProfitAction()
        {
            var times = GenerateRandomNumber(3, 5);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count - 1);
                TakeUserProfit(Testers[id]);

                Thread.Sleep(10);
            }
        }

        private void TakeUserProfit(string account)
        {
            var profitId = ProfitItemIds[ProfitType.CitizenWelfare];
            var voteProfit =
                Profit.GetProfitDetails(account, profitId);
            if (voteProfit.Equals(new ProfitDetails())) return;
            $"20% user vote profit details number: {voteProfit.Details}".WriteSuccessLine();

            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, profitId);
            if (profitAmount == 0)
                return;

            Logger.Info($"Profit amount: user {account} profit amount is {profitAmount}");
            //Profit.SetAccount(account);
            var profit = Profit.GetNewTester(account);
            var profitResult = profit.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });

            if (!(profitResult.InfoMsg is TransactionResultDto profitDto)) return;
            if (profitDto.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                Logger.Info(
                    $"Profit success - user {account} get vote profit from Id: {profitId}, value is: {profitAmount}");
        }

        private void UserVote(string account)
        {
            var id = GenerateRandomNumber(0, _candidatesExcludeMiners.Count - 1);
            var lockTime = GenerateRandomNumber(3, 36) * 30;
            var amount = GenerateRandomNumber(1, 5) * 5;

            UserVote(account, _candidatesExcludeMiners[id], lockTime, amount);
        }

        private void UserVote(string account, string candidatePublicKey, int lockTime, long amount)
        {
            var beforeBalance = Token.GetUserBalance(account);
            if (beforeBalance < amount) // balance not enough, bp transfer again
            {
                var token = Token.GetNewTester(BpNodes.First().Account);
                token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 10_0000,
                    To = Address.Parse(account),
                    Memo = $"Transfer for voting = {Guid.NewGuid()}"
                });
            }

            //Election.SetAccount(account);
            var election = Election.GetNewTester(account);
            election.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePublicKey = candidatePublicKey,
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime))
                    .Add(TimeSpan.FromHours(1))
                    .ToTimestamp()
            });

            var afterBalance = Token.GetUserBalance(account);
            if (beforeBalance == afterBalance + amount)
                Logger.Info(
                    $"Vote success - {account} vote candidate: {candidatePublicKey} with amount: {amount} lock time: {lockTime} days.");
        }

        public static void GetCandidates(ElectionContract election)
        {
            var candidatePublicKeys = election.CallViewMethod<Candidates>(ElectionMethod.GetCandidates, new Empty());
            _candidates = candidatePublicKeys.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
        }

        private void GetCandidatesExcludeCurrentMiners()
        {
            //query current miners
            var miners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();

            //query current candidates
            _candidatesExcludeMiners = new List<string>();
            _candidates.ForEach(o =>
            {
                if (!minersPublicKeys.Contains(o))
                {
                    _candidatesExcludeMiners.Add(o);
                }
            });
        }
    }
}