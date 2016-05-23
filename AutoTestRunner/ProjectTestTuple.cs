using System;
using MonoDevelop.Core.Serialization;
namespace Taiste.AutoTestRunner
{
    public class ProjectTestTuple
    {

        public ProjectTestTuple() { }
        
        public ProjectTestTuple(string project, string test) {
            Project = project;
            Test = test;
        }
        [ItemProperty]
        public string Project;
        [ItemProperty]
        public string Test;
    }
}

