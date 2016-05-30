using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;
using System.Collections.Generic;
using ICSharpCode.NRefactory.MonoCSharp;
using System.Threading;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Taiste.AutoTestRunner
{
    public class StartupHandler : CommandHandler
    {
        protected override void Run()
        {
            Observable.FromEventPattern<ProjectFileEventHandler, ProjectFileEventArgs>
                      (e => IdeApp.Workspace.FileChangedInProject += e,
                       e => IdeApp.Workspace.FileChangedInProject -= e)
                      .Buffer(TimeSpan.FromMilliseconds(500))
                      .Where(l => l.Any())
                      .ObserveOn(SynchronizationContext.Current)
                      .Select( p => p.Select(i => i.EventArgs))
                      .Subscribe((l) => RunTests(l), (e) => {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            
            });

            Observable.FromEventPattern<SolutionEventArgs>(e => IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += e,
                                        e => IdeApp.ProjectOperations.CurrentSelectedSolutionChanged -= e)
                      .Subscribe(_ => AddInPreferences.OnSolutionChanged());

        }

        async Task RunTests(IEnumerable<ProjectFileEventArgs> l)
        {
            if (l.Count() == 1 && l.First().First().ProjectFile.FilePath.FileName.ToLower().StartsWith("resource.designer"))
            {
                return;
            }

            var commonProject = l.Select(r => r.CommonProject).Distinct().First();

            var rootTest = UnitTestService.FindRootTest(commonProject.ParentSolution.RootFolder);

            HashSet<UnitTest> tests = new HashSet<UnitTest>();
            var rootGroup = rootTest as UnitTestGroup;
            foreach (var e in l)
            {
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
            }

            var coll = new CustomTestGroup(
                "Specified tests",
                rootTest.OwnerObject
            );

            var context = new MonoDevelop.Projects.ExecutionContext(
                Runtime.ProcessService.DefaultExecutionHandler, IdeApp.Workbench.ProgressMonitors.ConsoleFactory, IdeApp.Workspace.ActiveExecutionTarget);

            foreach (var test in tests)
            {

                var tmp = new CustomTestGroup("this isnt a real test", test.OwnerObject);

                // We're only calling this so that the owner object gets built properly.
                // This will fail to run the tests because it tries to find the test
                // by name again after building the project, in case it was removed.
                // Since this is a temporary test group, it won't find it and will just return.

                await UnitTestService.RunTest(tmp, context).Task;
                coll.Tests.Add(test);
            }

            // To get around this, run the tests again but don't build the owner object:
            UnitTestService.RunTest(coll, context, false);
        }
    }

    public class CustomTestGroup : UnitTestGroup
    {
        public CustomTestGroup(string name, WorkspaceObject owner) : base(name, owner) { }
    }
}

