using System;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using Gtk;
using System.Linq;
using MonoDevelop.UnitTesting;
using System.Collections.Generic;
namespace Taiste.AutoTestRunner
{
    public class SolutionOptions : OptionsPanel
    {

        const string EmptyChoice = "<None>";

        class ComboChoice
        {
            public string Text;
        }

        List<Tuple<ComboChoice, ComboChoice>> rows = new List<Tuple<ComboChoice, ComboChoice>>();

        ListStore store;
        TreeView list;
        public override Control CreatePanelWidget()
        {
            var solution = DataObject as Solution;
            Gtk.VBox box = new Gtk.VBox();

            if (solution == null)
            {
                return box; // something went very wrong
            }

            var scrollArea = new ScrolledWindow();

            store = new Gtk.ListStore(typeof(ComboChoice), typeof(ComboChoice));
            list = new Gtk.TreeView();
            list.Model = store;

            LoadConfig();

            var crt = new CellRendererComboBox();
            crt.Changed += GetOnChangedListener(0);
            var projectsColumn = list.AppendColumn("Project", crt, new TreeCellDataFunc(GetOnSetDataFunc(0, GetProjectNames)));
            projectsColumn.Expand = true;


            var testRenderer = new CellRendererComboBox();
            testRenderer.Changed += GetOnChangedListener(1);
            var testsColumn = list.AppendColumn("Tests to run", testRenderer, new TreeCellDataFunc(GetOnSetDataFunc(1, GetTestNames)));
            testsColumn.Expand = true;

            list.Selection.Changed += delegate
            {
                Gtk.TreeIter it;
                list.Selection.GetSelected(out it);
                Gtk.TreeViewColumn ccol;
                Gtk.TreePath path;
                list.GetCursor(out path, out ccol);
                list.SetCursor(path, ccol, true);
            };

            Button b = new Button();
            b.Label = "Add row";
            b.Clicked += (o, e) => {
                var project = new ComboChoice { Text = GetProjectNames()[0] };
                var test = new ComboChoice { Text = EmptyChoice };
                rows.Add(Tuple.Create(project, test));
                store.AppendValues(project,test);
            };

            box.Spacing = 6;
            scrollArea.Add(list);
            box.PackStart(scrollArea);
            box.PackStart(b,false,false,5);
            box.ShowAll();
            return box;

        }

        void LoadConfig() {
            var config = AddInPreferences.ProjectTestMap;
            if (config == null) {
                return;
            }
            foreach (var tuple in config) { 
                var project = new ComboChoice { Text = tuple.Project };
                var test = new ComboChoice { Text = tuple.Test };
                rows.Add(Tuple.Create(project, test));
                store.AppendValues(project, test);
            }
        }

        delegate void OnSetDataFunc(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter);


        OnSetDataFunc GetOnSetDataFunc(int index, Func<string[]> valueSource)
        {
            return (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) =>
            {
                var combo = cell as CellRendererComboBox;
                combo.Values = valueSource();
                var value = tree_model.GetValue(iter, index) as ComboChoice;
                combo.Text = value.Text;
               
            };
        }

        ComboSelectionChangedHandler GetOnChangedListener(int index) {
            return (object sender, ComboSelectionChangedArgs args) =>
            {
                Gtk.TreeIter iter;
                if (store.GetIter(out iter, new Gtk.TreePath(args.Path)))
                {
                    ComboChoice mt = (ComboChoice)store.GetValue(iter, index);
                    if (args.Active != -1)
                    {
                        mt.Text = args.ActiveText;
                    }
                }
            }; 
        
        }

        static Gtk.Label CreateLabel(string text)
        {
            var label = new Gtk.Label(text);
            label.ModifyFont(new Pango.FontDescription { Weight = Pango.Weight.Bold });
            return label;
        }

        string[] GetProjectNames()
        {
            var solution = DataObject as Solution;
            return solution.GetAllProjects().Select(p => p.Name).ToArray();
        }

        string[] GetTestNames()
        {
            var solution = DataObject as Solution;
            var rootTest = UnitTestService.FindRootTest(solution.RootFolder) as UnitTestGroup;
            var tests = rootTest?.Tests;
            return tests != null ? tests.Select(t => t.Name).Concat(new string[] { EmptyChoice })?.ToArray() : new string[] { EmptyChoice };
        }

        public override void ApplyChanges()
        {
            AddInPreferences.ProjectTestMap = rows.Where(kvp => kvp.Item2.Text != EmptyChoice).Select(t => new ProjectTestTuple(t.Item1.Text, t.Item2.Text)).ToList();
            AddInPreferences.SaveConfig();
        }
    }
}

