using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using VstsSyncMigrator.Engine.Configuration.Processing;

namespace VstsSyncMigrator.Engine
{
    public class GitRepoMigrateContext : ProcessingContextBase
    {
        private GitRepoMigrationConfig _config;

        public GitRepoMigrateContext(MigrationEngine me, GitRepoMigrationConfig config) : base(me, config)
        {
            _config = config;
        }

        public override string Name => "GitRepoMigrateContext";

        internal override void InternalExecute()
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<Uri> repoUris;

            if (_config.SourceRepositoryUrls.Count() == 1 && _config.SourceRepositoryUrls.Single().Trim() == "*")
            {
                repoUris = GetAllRepositoryUrls().Result;
            }
            else if (_config.SourceRepositoryUrls.Any())
            {
                repoUris = _config.SourceRepositoryUrls.Select(sr => new Uri(sr));
            }
            else
            {
                Trace.WriteLine("No repositories found. Stopping.");

                return;
            }



        }

        private async Task<IEnumerable<Uri>> GetAllRepositoryUrls()
        {
            var ret = new List<Uri>();

            var gitClient = me.Source.Collection.GetClient<GitHttpClient>();

            var result = await gitClient.GetRepositoriesAsync(me.Target.Name, true, true, true);

            return result.Select(r => new Uri(r.Url));
        }
    }
}
