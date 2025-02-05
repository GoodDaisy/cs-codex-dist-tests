﻿using CodexPlugin;
using Discord.WebSocket;

namespace BiblioTech.Commands
{
    public class DeploymentsCommand : BaseCommand
    {
        private readonly DeploymentsFilesMonitor monitor;

        public DeploymentsCommand(DeploymentsFilesMonitor monitor)
        {
            this.monitor = monitor;
        }

        public override string Name => "deployments";
        public override string StartingMessage => "Fetching deployments information...";
        public override string Description => "Lists active TestNet deployments";

        protected override async Task Invoke(SocketSlashCommand command)
        {
            var deployments = monitor.GetDeployments();

            if (!deployments.Any())
            {
                await command.FollowupAsync("No deployments available.");
                return;
            }

            await command.FollowupAsync($"Deployments: {string.Join(", ", deployments.Select(FormatDeployment))}");
        }

        private string FormatDeployment(CodexDeployment deployment)
        {
            var m = deployment.Metadata;
            return $"{m.Name} ({m.StartUtc.ToString("o")})";
        }
    }
}
