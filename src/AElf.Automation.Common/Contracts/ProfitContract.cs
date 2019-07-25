using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.Common.Contracts
{
    public enum ProfitMethod
    {
        //action
        InitializeProfitContract,
        CreateProfitItem,
        RegisterSubProfitItem,
        AddWeight,
        SubWeight,
        AddWeights,
        ReleaseProfit,
        AddProfits,
        ClaimProfits,

        //view
        GetManagingSchemeIds,
        GetSchemeAddress,
        GetScheme,
        GetReleasedProfitsInformation,
        GetProfitDetails,
        GetProfitItem,
        GetProfitAmount
    }

    public enum SchemeType
    {
        Treasury,

        MinerReward,
        BackupSubsidy,
        CitizenWelfare,

        MinerBasicReward,
        VotesWeightReward,
        ReElectionReward
    }

    public class ProfitContract : BaseContract<ProfitMethod>
    {
        public static Dictionary<SchemeType, Scheme> Schemes { get; set; }

        public ProfitContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.Profit", callAddress)
        {
        }

        public ProfitContract(IApiHelper apiHelper, string callAddress, string contractAddress) :
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public void GetTreasurySchemes(string treasuryContractAddress)
        {
            if (Schemes != null && Schemes.Count == 7)
                return;
            Schemes = new Dictionary<SchemeType, Scheme>();
            var treasuryContract = new TreasuryContract(ApiHelper, CallAddress, treasuryContractAddress);
            var treasurySchemeId =
                treasuryContract.CallViewMethod<Hash>(TreasuryMethod.GetTreasurySchemeId, new Empty());
            var treasuryScheme = CallViewMethod<Scheme>(ProfitMethod.GetScheme, treasurySchemeId);
            Schemes.Add(SchemeType.Treasury, treasuryScheme);
            var minerRewardScheme =
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, treasuryScheme.SubSchemes[0].SchemeId);
            Schemes.Add(SchemeType.MinerReward, minerRewardScheme);

            Schemes.Add(SchemeType.BackupSubsidy,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, treasuryScheme.SubSchemes[1].SchemeId));
            Schemes.Add(SchemeType.CitizenWelfare,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, treasuryScheme.SubSchemes[2].SchemeId));
            Schemes.Add(SchemeType.MinerBasicReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[0].SchemeId));
            Schemes.Add(SchemeType.VotesWeightReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[1].SchemeId));
            Schemes.Add(SchemeType.ReElectionReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[2].SchemeId));
            Logger.Info("Scheme collection info:");
            foreach (var (key, value) in Schemes)
            {
                Logger.Info($"Name: {key}, SchemeId: {value.SchemeId}");
            }
        }

        public ProfitDetails GetProfitDetails(string voteAddress, Hash profitId)
        {
            var result =
                CallViewMethod<ProfitDetails>(ProfitMethod.GetProfitDetails,
                    new GetProfitDetailsInput
                    {
                        Beneficiary = Address.Parse(voteAddress),
                        SchemeId = profitId
                    });
            return result;
        }

        public long GetProfitAmount(string account, Hash schemeId)
        {
            var newTester = GetNewTester(account);
            return newTester.CallViewMethod<SInt64Value>(ProfitMethod.GetProfitAmount, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = "ELF"
            }).Value;
        }

        public Address GetSchemeAddress(Hash schemeId, long period)
        {
            var result = CallViewMethod<Address>(ProfitMethod.GetSchemeAddress,
                new SchemePeriod
                {
                    SchemeId = schemeId,
                    Period = period
                });

            return result;
        }
    }
}