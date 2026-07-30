using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.About;
using MonocleViewExtension.BetterSave;
using MonocleViewExtension.Core;
using MonocleViewExtension.FancyPaste;
using MonocleViewExtension.Foca;
using MonocleViewExtension.GraphInformation;
using MonocleViewExtension.GraphResizerer;
using MonocleViewExtension.InlineNodeConnectomatic;
using MonocleViewExtension.MonocleSettings;
using MonocleViewExtension.NodeDocumentation;
using MonocleViewExtension.NodeSwapper;
using MonocleViewExtension.PackageUsage;
using MonocleViewExtension.SimpleSearch;
using MonocleViewExtension.StandardViews;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension
{
    public class MonocleViewExtension : IViewExtension
    {
        public string UniqueId => "5A256B35-BD09-423C-82A1-372957143927";
        public string Name => "Monocle View Extension";

        private readonly List<IMonocleFeature> _features = new List<IMonocleFeature>();
        private MonocleContext _ctx;

        public void Dispose()
        {
            foreach (var feature in _features)
            {
                try
                {
                    feature.Dispose();
                }
                catch (Exception e)
                {
                    _ctx?.Log.Warn($"Could not clean up '{feature.Name}'.", e);
                }
            }
            _features.Clear();

            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving -= AssemblyLoadContext_Resolving;
            MonocleContext.Clear();
            _ctx = null;
        }

        public void Startup(ViewStartupParams viewStartupParams)
        {
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += AssemblyLoadContext_Resolving;
        }

        private Assembly AssemblyLoadContext_Resolving(System.Runtime.Loader.AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var assemblyNameStr = new AssemblyName(assemblyName.Name).Name + ".dll";
            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray().FirstOrDefault(x => x.EndsWith(assemblyNameStr));
            if (resourceName == null)
            {
                return null;
            }
            using (var stream = Globals.ExecutingAssembly.GetManifestResourceStream(resourceName))
            {
                return context.LoadFromStream(stream);
            }
        }

        public void Loaded(ViewLoadedParams p)
        {
            /*if the user is holding down the left shift key, don't load monocle. I added this because I needed it for when I record videos that shouldn't have packages loaded.
            And yes. this is a deep reference to my roots in AutoCAD, https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/How-to-reset-AutoCAD-to-defaults.html
            */
            if (Keyboard.IsKeyDown(Key.LeftShift)) return;

            _ctx = MonocleContext.Create(p);

            //add the top-level menu to the dynamo ribbon
            var monocleMenuItem = new MenuItem { Header = "🧐 monocle" };
            p.dynamoMenu.Items.Insert(6, monocleMenuItem);

            _features.AddRange(BuildFeatures());

            foreach (var feature in _features)
            {
                try
                {
                    feature.Register(_ctx, monocleMenuItem);
                }
                catch (Exception e)
                {
                    // One tool failing to load must not cost the user the rest of the menu.
                    _ctx.Log.Error($"'{feature.Name}' failed to load and will be unavailable.", e);
                }
            }

            ScaffoldTheJacobSmallSpecial(p);

            /*if the user has plugins loaded in Revit (or otherwise) that use a toolkit called "DevExpress",
            we fix the overrides that toolkit forces on the app.
            A popular example of this is KiwiCodes Family Browser R3.
            This code will fix it for all of the Dynamo UI.
            */
            Compatibility.CheckForDevExpress();
            Compatibility.FixThemesForDevExpress(p.DynamoWindow);
        }

        /// <summary>
        /// Menu order is the registration order, so this list is also the layout of the monocle menu.
        /// </summary>
        private IEnumerable<IMonocleFeature> BuildFeatures()
        {
            StandardViewsViewModel standardViews = null;

            return new IMonocleFeature[]
            {
                new DelegateFeature("About", (ctx, menu) => AboutCommand.AddMenuItem(menu, ctx.LoadedParams)),
                new DelegateFeature("Package Usage", (ctx, menu) => PackageUsageCommand.AddMenuItem(menu, ctx.LoadedParams)),
                new DelegateFeature("Graph Resizerer", (ctx, menu) => GraphResizererCommand.AddMenuItem(menu, ctx.LoadedParams)),
                new DelegateFeature("Node Swapper", (ctx, menu) => NodeSwapperCommand.AddMenuItem(menu, ctx.LoadedParams)),
                new FocaFeature(),
                new InlineNodeConnectomaticFeature(),
                new DelegateFeature("Simple Search", (ctx, menu) => SimpleSearchCommand.AddMenuItem(ctx.LoadedParams, menu, this)),
                new DelegateFeature("Standard Views",
                    (ctx, menu) => standardViews = StandardViewsCommand.EnableStandardViews(ctx.LoadedParams),
                    () => standardViews?.Dispose()),
                new DelegateFeature("Settings", (ctx, menu) => MonocleSettingsCommand.AddMenuItem(menu, ctx)),
                new DelegateFeature("Fancy Paste", (ctx, menu) => FancyPasteCommand.AddMenuItem(ctx.LoadedParams)),
                new DelegateFeature("Better Save", (ctx, menu) => BetterSaveCommand.AddMenuItem(ctx.LoadedParams)),
                new DelegateFeature("Graph Information", (ctx, menu) => GraphInformationCommand.AddMenuItem(menu, ctx.LoadedParams)),
                new DelegateFeature("Node Documentation", (ctx, menu) => NodeDocumentationCommand.AddMenuItem(menu, ctx.LoadedParams))
            };
        }

        public void Shutdown()
        {
            // Backstop: the settings dialog saves on apply, but a session that only toggled a
            // menu checkbox still has changes to persist.
            _ctx?.Settings.Save();
        }

        internal void ScaffoldTheJacobSmallSpecial(ViewLoadedParams p)
        {
            MenuItem myDynamoNoWorkie = new MenuItem
            {
                Header = "My Dynamo is not loading correctly."
            };

            MenuItem jacobSmallSpecial = new MenuItem
            {
                Header = "Invoke the Jacob Small Special™️ ??"
            };
            var img = new System.Windows.Controls.Image
            {
                Source = ImageUtils.LoadImage(Assembly.GetExecutingAssembly(), "smalls.JPG"),
                Height = 32,
                Width = 32,
                Stretch = Stretch.Uniform
            };
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Children.Add(img);
            wrapPanel.Children.Add(jacobSmallSpecial);

            jacobSmallSpecial.Click += (sender, args) =>
            {
                Process.Start(@"https://forum.dynamobim.com/t/2022-1-latest-revit-update-broke-dynamo/73412/3");
            };

            myDynamoNoWorkie.Items.Add(wrapPanel);

            //in Dynamo 2.18+, the team decided I can't add things to the help menu the old way, this fixes that.
            var allMenus = p.dynamoMenu.Items.OfType<MenuItem>();
            var helpMenu = allMenus.FirstOrDefault(m => m.Name.Equals("HelpMenu"));

            helpMenu?.Items.Add(myDynamoNoWorkie);
        }
    }
}
