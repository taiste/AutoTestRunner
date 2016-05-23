using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;
using System.Collections.Generic;
using ICSharpCode.NRefactory.MonoCSharp;
namespace Taiste.AutoTestRunner
{
    public class StartupHandler : CommandHandler
    {
        protected override void Run()
        {
            IdeApp.Workspace.FileChangedInProject += Workspace_FileChangedInProject;

        }

        async void Workspace_FileChangedInProject(object sender, ProjectFileEventArgs e)
        {
            if (e.Count == 1 && e.First().ProjectFile.FilePath.FileName.ToLower().StartsWith("resource.designer"))
            {
                return;
            }

            var rootTest = UnitTestService.FindRootTest(e.CommonProject.ParentSolution.RootFolder);

            HashSet<UnitTest> tests = new HashSet<UnitTest>();
            var rootGroup = rootTest as UnitTestGroup;

            foreach (var file in e)
            {
                foreach (var testGroup in rootGroup.Tests)
                {
                    if (AddInPreferences.ProjectTestMap.Any(p => p.Project == file.Project.Name && p.Test == testGroup.Name))
                    {
                        tests.Add(testGroup);
                    }
                }
            }

            // Construct a unit test group by calling a protected constructor 
            // to avoid a null pointer exception related to the test owner object in NUnit. 
            var cons = typeof(UnitTestGroup)
                .GetConstructor(System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.CreateInstance
                                | System.Reflection.BindingFlags.Instance,
                                null,
                                new[] { typeof(string), typeof(WorkspaceObject) },
                                null);
            var coll = cons.Invoke(
                new object[] {
                "Specified tests",
                rootTest.OwnerObject
            }) as UnitTestGroup;

            foreach (var test in tests)
            {
                coll.Tests.Add(test);
            }

            var context = new ExecutionContext(
                Runtime.ProcessService.DefaultExecutionHandler, IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);

            // We're only calling this so that the owner object gets built properly.
            // This will fail to run the tests because it tries to find the test
            // by name again after building the project, in case it was removed.
            // Since this is a temporary test group, it won't find it and will just return.
            await UnitTestService.RunTest(coll, context).Task;

            // To get around this, run the tests again but don't build the owner object:
            UnitTestService.RunTest(coll, context, false);

        }
    }
}

