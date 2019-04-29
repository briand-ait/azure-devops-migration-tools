using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VstsSyncMigrator.Engine
{
    public interface ITfsProcessingContext
    {
       

        string Name { get; }
        ProcessingStatus Status { get; }
        void Execute();
    }


}
