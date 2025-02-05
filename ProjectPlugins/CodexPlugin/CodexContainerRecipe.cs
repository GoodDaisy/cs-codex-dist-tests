﻿using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        private readonly MarketplaceStarter marketplaceStarter = new MarketplaceStarter();

        private const string DefaultDockerImage = "codexstorage/nim-codex:latest-dist-tests";
        public const string ApiPortTag = "codex_api_port";
        public const string ListenPortTag = "codex_listen_port";
        public const string MetricsPortTag = "codex_metrics_port";
        public const string DiscoveryPortTag = "codex_discovery_port";

        // Used by tests for time-constraint assertions.
        public static readonly TimeSpan MaxUploadTimePerMegabyte = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan MaxDownloadTimePerMegabyte = TimeSpan.FromSeconds(2.0);

        public override string AppName => "codex";
        public override string Image => GetDockerImage();

        public static string DockerImageOverride { get; set; } = string.Empty;

        protected override void Initialize(StartupConfig startupConfig)
        {
            SetResourcesRequest(milliCPUs: 100, memory: 100.MB());
            SetResourceLimits(milliCPUs: 4000, memory: 12.GB());

            var config = startupConfig.Get<CodexStartupConfig>();

            AddExposedPortAndVar("CODEX_API_PORT", ApiPortTag);
            AddEnvVar("CODEX_API_BINDADDR", "0.0.0.0");

            var dataDir = $"datadir{ContainerNumber}";
            AddEnvVar("CODEX_DATA_DIR", dataDir);
            AddVolume($"codex/{dataDir}", GetVolumeCapacity(config));

            AddExposedPortAndVar("CODEX_DISC_PORT", DiscoveryPortTag);
            AddEnvVar("CODEX_LOG_LEVEL", config.LogLevelWithTopics());

            // This makes the node announce itself to its local (pod) IP address.
            AddEnvVar("NAT_IP_AUTO", "true");

            var listenPort = AddExposedPort(ListenPortTag);
            AddEnvVar("CODEX_LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{listenPort.Number}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddEnvVar("CODEX_BOOTSTRAP_NODE", config.BootstrapSpr);
            }
            if (config.StorageQuota != null)
            {
                AddEnvVar("CODEX_STORAGE_QUOTA", config.StorageQuota.SizeInBytes.ToString()!);
            }
            if (config.BlockTTL != null)
            {
                AddEnvVar("CODEX_BLOCK_TTL", config.BlockTTL.ToString()!);
            }
            if (config.BlockMaintenanceInterval != null)
            {
                AddEnvVar("CODEX_BLOCK_MI", Convert.ToInt32(config.BlockMaintenanceInterval.Value.TotalSeconds).ToString());
            }
            if (config.BlockMaintenanceNumber != null)
            {
                AddEnvVar("CODEX_BLOCK_MN", config.BlockMaintenanceNumber.ToString()!);
            }
            if (config.MetricsEnabled)
            {
                var metricsPort = AddInternalPort(MetricsPortTag);
                AddEnvVar("CODEX_METRICS", "true");
                AddEnvVar("CODEX_METRICS_ADDRESS", "0.0.0.0");
                AddEnvVar("CODEX_METRICS_PORT", metricsPort);
                AddPodAnnotation("prometheus.io/scrape", "true");
                AddPodAnnotation("prometheus.io/port", metricsPort.Number.ToString());
            }

            if (config.SimulateProofFailures != null)
            {
                AddEnvVar("CODEX_SIMULATE_PROOF_FAILURES", config.SimulateProofFailures.ToString()!);
            }

            if (config.MarketplaceConfig != null)
            {
                var mconfig = config.MarketplaceConfig;
                var gethStart = mconfig.GethNode.StartResult;
                var ip = gethStart.Container.Pod.PodInfo.Ip;
                var port = gethStart.WsPort.Number;
                var marketplaceAddress = mconfig.CodexContracts.Deployment.MarketplaceAddress;

                AddEnvVar("CODEX_ETH_PROVIDER", $"ws://{ip}:{port}");
                AddEnvVar("CODEX_MARKETPLACE_ADDRESS", marketplaceAddress);
                AddEnvVar("CODEX_PERSISTENCE", "true");

                // Custom scripting in the Codex test image will write this variable to a private-key file,
                // and pass the correct filename to Codex.
                var mStart = marketplaceStarter.Start();
                AddEnvVar("PRIV_KEY", mStart.PrivateKey);
                Additional(mStart);

                if (config.MarketplaceConfig.IsValidator)
                {
                   AddEnvVar("CODEX_VALIDATOR", "true");
                }
            }

            if(!string.IsNullOrEmpty(config.NameOverride))
            {
                AddEnvVar("CODEX_NODENAME", config.NameOverride);
            }
        }

        private ByteSize GetVolumeCapacity(CodexStartupConfig config)
        {
            if (config.StorageQuota != null) return config.StorageQuota;
            // Default Codex quota: 8 Gb, using +20% to be safe.
            return 8.GB().Multiply(1.2);
        }

        private string GetDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("CODEXDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            if (!string.IsNullOrEmpty(DockerImageOverride)) return DockerImageOverride;
            return DefaultDockerImage;
        }
    }
}
