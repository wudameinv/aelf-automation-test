using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Genesis;
using AElf.Types;
using Configuration;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionExecuteLimit
    {
        private readonly IApiHelper _apiHelper;
        private readonly string _account;
        private readonly ContractTesterFactory _stub;
        private readonly NodeTransactionOption _nodeTransactionOption;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public TransactionExecuteLimit(string url, string account)
        {
            _account = account;

            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _apiHelper = new WebApiHelper(url, keyStorePath);
            _stub = new ContractTesterFactory(url, keyStorePath);
            _nodeTransactionOption = ConfigInfoHelper.Config.NodeTransactionOption;
        }
        
        public bool WhetherEnableTransactionLimit()
        {
            return _nodeTransactionOption.EnableLimit;
        }
        
        public void SetExecutionSelectTransactionLimit()
        {
            var configurationStub = AsyncHelper.RunSync(GetConfigurationContractStub);
            var limitCount = _nodeTransactionOption.MaxTransactionSelect;
            AsyncHelper.RunSync(() => SetSelectTransactionLimit(configurationStub, limitCount));
        }

        private async Task<ConfigurationContainer.ConfigurationStub> GetConfigurationContractStub()
        {
            var chainStatus = await _apiHelper.ApiService.GetChainStatus();
            var genesisContractAddress = chainStatus.GenesisContractAddress;
            _logger.Info($"Genesis contract address: {genesisContractAddress}");

            var basicZeroStub =
                _stub.Create<BasicContractZeroContainer.BasicContractZeroStub>(Address.Parse(genesisContractAddress),
                    _account);
            var configurationAddress =
                await basicZeroStub.GetContractAddressByName.CallAsync(
                    GenesisContract.NameProviderInfos[NameProvider.Configuration]);
            _logger.Info($"Configuration contract address: {configurationAddress.GetFormatted()}");

            var configurationStub =
                _stub.Create<ConfigurationContainer.ConfigurationStub>(configurationAddress, _account);

            return configurationStub;
        }

        private async Task SetSelectTransactionLimit(ConfigurationContainer.ConfigurationStub configurationStub,
            int limitCount)
        {
            var beforeResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            _logger.Info($"Old transaction limit number: {beforeResult.Value}");

            if (beforeResult.Value == limitCount)
                return;

            await configurationStub.SetBlockTransactionLimit.SendAsync(new Int32Value
            {
                Value = limitCount
            });
            var afterResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            _logger.Info($"New transaction limit number: {afterResult.Value}");
            if (afterResult.Value == limitCount)
                _logger.Info("Transaction limit set successful.");
        }
    }
}