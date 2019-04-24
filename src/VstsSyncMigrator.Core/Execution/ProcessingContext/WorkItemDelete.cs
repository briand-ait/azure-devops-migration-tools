﻿using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using VstsSyncMigrator.Engine.Configuration.Processing;

namespace VstsSyncMigrator.Engine
{
    public class WorkItemDelete : ProcessingContextBase
    {


        public WorkItemDelete(MigrationEngine me, ITfsProcessingConfig config) : base(me, config)
        {

        }

        public override string Name
        {
            get
            {
                return "WorkItemDelete";
            }
        }

        internal override void InternalExecute()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //////////////////////////////////////////////////
            var targetStore = new WorkItemStoreContext(me.Target, WorkItemStoreFlags.BypassRules);
            var tfsqc = new TfsQueryContext(targetStore);
            tfsqc.AddParameter("TeamProject", me.Target.Name);
            tfsqc.Query = string.Format(@"SELECT [System.Id] FROM WorkItems WHERE  [System.TeamProject] = @TeamProject  AND [System.AreaPath] UNDER '{0}\_DeleteMe'", me.Target.Name);
            var  workitems = tfsqc.Execute();
            Trace.WriteLine(string.Format("Update {0} work items?", workitems.Count));
            //////////////////////////////////////////////////
            var current = workitems.Count;
            //int count = 0;
            //long elapsedms = 0;
            var tobegone = (from WorkItem wi in workitems where wi.AreaPath.Contains("_DeleteMe")  select wi.Id).ToList();

            foreach (var begone in tobegone)
            {
                targetStore.Store.DestroyWorkItems(new List<int>() { begone });
                Trace.WriteLine(string.Format("Deleted {0}", begone));
            }

            
            //////////////////////////////////////////////////
            stopwatch.Stop();
            Console.WriteLine(@"DONE in {0:%h} hours {0:%m} minutes {0:s\:fff} seconds", stopwatch.Elapsed);
        }

    }
}