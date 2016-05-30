using System;
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace Taiste.AutoTestRunner
{
    class AddinConfig
    {
        [ItemProperty]
        public List<ProjectTestTuple> ProjectTestMap;
    }


    public static class AddInPreferences
    {


        public static List<ProjectTestTuple> ProjectTestMap
        {
            get { return GetConfig().ProjectTestMap; }
            set { GetConfig().ProjectTestMap = value; }
        }


        private static AddinConfig configuration;
        private static readonly DataContext dataContext = new DataContext();


        public static void OnSolutionChanged() {
            configuration = null;
        }

        static string ConfigFile
        {
            get { return IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory.Combine("AutoTestRunnerConf.xml"); }
        }

        public static void SaveConfig()
        {
            if (configuration != null)
            {
                XmlDataSerializer s = new XmlDataSerializer(dataContext);
                using (var wr = new XmlTextWriter(File.CreateText(ConfigFile)))
                {
                    wr.Formatting = Formatting.Indented;
                    s.Serialize(wr, configuration, typeof(AddinConfig));
                }
            }
        }



        private static AddinConfig GetConfig()
        {
            if (configuration != null)
            {
                return configuration;
            }
            if (File.Exists(ConfigFile))
            {
                try
                {
                    XmlDataSerializer s = new XmlDataSerializer(dataContext);
                    using (var reader = File.OpenText(ConfigFile))
                    {
                        configuration = (AddinConfig)s.Deserialize(reader, typeof(AddinConfig));
                    }
                }
                catch (Exception e)
                {
                    ((FilePath)ConfigFile).Delete();
                }
            }
            if (configuration == null)
            {
                configuration = new AddinConfig();
            }
            return configuration;
        }
    }
}

