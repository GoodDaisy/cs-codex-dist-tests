﻿using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace CodexContractsPlugin
{
    public class ContractsContainerInfoExtractor
    {
        private readonly ILog log;
        private readonly IStartupWorkflow workflow;
        private readonly RunningContainer container;

        public ContractsContainerInfoExtractor(ILog log, IStartupWorkflow workflow, RunningContainer container)
        {
            this.log = log;
            this.workflow = workflow;
            this.container = container;
        }

        public string ExtractMarketplaceAddress()
        {
            log.Debug();
            var marketplaceAddress = Retry(FetchMarketplaceAddress);
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new InvalidOperationException("Unable to fetch marketplace account from codex-contracts node. Test infra failure.");

            return marketplaceAddress;
        }

        public string ExtractMarketplaceAbi()
        {
            log.Debug();
            var marketplaceAbi = Retry(FetchMarketplaceAbi);
            if (string.IsNullOrEmpty(marketplaceAbi)) throw new InvalidOperationException("Unable to fetch marketplace artifacts from codex-contracts node. Test infra failure.");

            return marketplaceAbi;
        }

        private string FetchMarketplaceAddress()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceAddressFilename);
            var marketplace = JsonConvert.DeserializeObject<MarketplaceJson>(json);
            return marketplace!.address;
        }

        private string FetchMarketplaceAbi()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceArtifactFilename);

            var artifact = JObject.Parse(json);
            var abi = artifact["abi"];
            return abi!.ToString(Formatting.None);
        }

        private static string Retry(Func<string> fetch)
        {
            return Time.Retry(fetch, nameof(ContractsContainerInfoExtractor));
        }
    }

    public class MarketplaceJson
    {
        public string address { get; set; } = string.Empty;
    }
}
