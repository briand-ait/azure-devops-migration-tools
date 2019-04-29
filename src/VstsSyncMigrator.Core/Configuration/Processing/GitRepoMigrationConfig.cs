using System;
using System.Collections.Generic;
using VstsSyncMigrator.Engine.Configuration.Processing;

namespace VstsSyncMigrator.Engine.Configuration.Processing {
    public class GitRepoMigrationConfig : ITfsProcessingConfig
    {
        public IList<string> SourceRepositoryUrls { get; set; }
        public bool Enabled { get; set; }
        public Type Processor => typeof(GitRepoMigrateContext);
        
        public bool IsProcessorCompatible(IReadOnlyList<ITfsProcessingConfig> otherProcessors)
        {
            return true;
        }
    }
}