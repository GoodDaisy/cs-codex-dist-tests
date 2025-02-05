﻿using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainers[] DeployCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            return Plugin(ci).DeployCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, RunningContainer[] containers)
        {
            // ew, clean this up.
            var rcs = new RunningContainers(null!, containers.First().Pod, containers);
            return WrapCodexContainers(ci, new[] { rcs });
        }

        public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, RunningContainers[] containers)
        {
            return Plugin(ci).WrapCodexContainers(ci, containers);
        }

        public static ICodexNode StartCodexNode(this CoreInterface ci)
        {
            return ci.StartCodexNodes(1)[0];
        }

        public static ICodexNode StartCodexNode(this CoreInterface ci, Action<ICodexSetup> setup)
        {
            return ci.StartCodexNodes(1, setup)[0];
        }

        public static ICodexNodeGroup StartCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            var rc = ci.DeployCodexNodes(number, setup);
            var result = ci.WrapCodexContainers(rc);
            Plugin(ci).WireUpMarketplace(result, setup);
            return result;
        }

        public static ICodexNodeGroup StartCodexNodes(this CoreInterface ci, int number)
        {
            return ci.StartCodexNodes(number, s => { });
        }

        private static CodexPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexPlugin>();
        }
    }
}
