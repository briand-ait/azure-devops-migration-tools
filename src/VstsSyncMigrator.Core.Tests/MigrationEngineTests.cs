using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VstsSyncMigrator.Engine;
using VstsSyncMigrator.Engine.Configuration;

namespace _VstsSyncMigrator.Engine.Tests
{
    [TestClass]
    public class MigrationEngineTests
    {
        [TestMethod]
        public void TestEngineCreation()
        {
            var ec = EngineConfiguration.GetDefault();
            var me = new MigrationEngine(ec);
        }

        [TestMethod]
        public void TestEngineExecuteEmptyProcessors()
        {
            var ec = EngineConfiguration.GetDefault();
            ec.Processors.Clear();
            var me = new MigrationEngine(ec);
            me.Run();

        }

        [TestMethod]
        public void TestEngineExecuteEmptyFieldMaps()
        {
            var ec = EngineConfiguration.GetDefault();
            ec.Processors.Clear();
            ec.FieldMaps.Clear();
            var me = new MigrationEngine(ec);
            me.Run();
        }


    }
}
