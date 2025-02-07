using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Graph.Annotations;
using Dynamo.Logging;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using MonocleViewExtension.SimpleSearch;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.Foca
{
    public class FocaViewModel : ViewModelBase
    {
        public FocaModel Model { get; set; }

        public FocaView View;

        public DelegateCommand CreateGroup { get; set; }
        public DelegateCommand MouseEnter { get; set; }
        public DelegateCommand AlignClick { get; set; }
        public DelegateCommand ToolboxClick { get; set; }


        private double _width;
        public double Width
        {
            get => _width;
            set { _width = value; RaisePropertyChanged(nameof(Width)); }
        }

        private double _height;
        public double Height
        {
            get => _height;
            set { _height = value; RaisePropertyChanged(nameof(Height)); }
        }

        private Thickness _thickness;
        public Thickness Thickness
        {
            get => _thickness;
            set { _thickness = value; RaisePropertyChanged(nameof(Thickness)); }
        }

        private double _colorWheelHeight;
        public double ColorWheelHeight
        {
            get => _colorWheelHeight;
            set { _colorWheelHeight = value; RaisePropertyChanged(nameof(ColorWheelHeight)); }
        }

        private Thickness _colorWheelMargin;
        public Thickness ColorWheelMargin
        {
            get => _colorWheelMargin;
            set { _colorWheelMargin = value; RaisePropertyChanged(nameof(ColorWheelMargin)); }
        }

        private Canvas _expansionBay;
        public Canvas ExpansionBay
        {
            get => _expansionBay;
            set { _expansionBay = value; RaisePropertyChanged(nameof(ExpansionBay)); }
        }

        private bool _focaVisible;
        public bool FocaVisible
        {
            get => _focaVisible;
            set { _focaVisible = value; RaisePropertyChanged(nameof(FocaVisible)); }
        }

        private double _multiSelect;
        public double MultiSelect
        {
            get => _multiSelect;
            set { _multiSelect = value; RaisePropertyChanged(nameof(MultiSelect)); }
        }

        private bool _colorWheelVisibility;
        public bool ColorWheelVisibility
        {
            get => _colorWheelVisibility;
            set { _colorWheelVisibility = value; RaisePropertyChanged(nameof(ColorWheelVisibility)); }
        }

        private int _combineVisibility;
        public int CombineVisibility
        {
            get => _combineVisibility;
            set { _combineVisibility = value; RaisePropertyChanged(nameof(CombineVisibility)); }
        }
        private int _dropdownVisibility;
        public int DropdownVisibility
        {
            get => _dropdownVisibility;
            set { _dropdownVisibility = value; RaisePropertyChanged(nameof(DropdownVisibility)); }
        }
        private int _listPowVisibility;
        public int ListPowVisibility
        {
            get => _listPowVisibility;
            set { _listPowVisibility = value; RaisePropertyChanged(nameof(ListPowVisibility)); }
        }
        private int _nodeSwapVisibility;
        public int NodeSwapVisibility
        {
            get => _nodeSwapVisibility;
            set { _nodeSwapVisibility = value; RaisePropertyChanged(nameof(NodeSwapVisibility)); }
        }

        public FocaViewModel(FocaModel model)
        {
            Model = model;
            model.LoadedParams.SelectionCollectionChanged += LoadedParamsOnSelectionCollectionChanged;

            //set color wheel size
            ColorWheelHeight = 48;
            ColorWheelMargin = new Thickness(-36);
            ColorWheelVisibility = false;
            CombineVisibility = 0;
            DropdownVisibility = 0;
            ListPowVisibility = 0;
            NodeSwapVisibility = 0;

            //commands
            MouseEnter = new DelegateCommand(OnMouseEnter);
            CreateGroup = new DelegateCommand(OnCreateGroup);
            AlignClick = new DelegateCommand(OnAlignClick);

            ToolboxClick = new DelegateCommand(OnToolboxClick);
        }

      

        public void OnMouseEnter(object o)
        {
            ColorWheelVisibility = true;
            ColorWheelHeight = 60;
            ColorWheelMargin = new Thickness(-48);
        }

        public void OnCreateGroup(object o)
        {
            Model.CreateGroup(o.ToString());
        }

        public void OnAlignClick(object o)
        {
            Model.AlignSelected(o.ToString());
        }

        public void OnToolboxClick(object o)
        {
            Model.ToolBoxCommand(o.ToString());
        }

        private void LoadedParamsOnSelectionCollectionChanged(NotifyCollectionChangedEventArgs obj)
        {
#if DEBUG
            try
            {
                //test TODO: Verify implementation for release
                SimpleSearchCommand.SimpleSearchPopup.IsOpen = false;
            }
            catch (Exception)
            {
                //suppress for now
            }
#endif


            try
            {
                if (!Globals.IsFocaEnabled)
                {
                    ExpansionBay?.Children.Remove(View);
                    return;
                }


                var count = Model.LoadedParams.CurrentWorkspaceModel.CurrentSelection.Count();

                FocaVisible = Model.ShowWidget();
                MultiSelect = count >= 2 ? 1.0 : 0.0;

                //remove old
                ExpansionBay?.Children.Remove(View);
                ExpansionBay = Model.GetExpansionBay();
                ExpansionBay?.Children.Add(View);
                var rec = Model.WrapNodes();
                Width = rec.Width;
                Height = rec.Height;
                Thickness = Model.GetThickness();

                CombineVisibility = count > 1 ? 1 : 0;
                DropdownVisibility = count == 1 ? 1 : 0;
                ListPowVisibility = count == 1 ? 1 : 0;
                NodeSwapVisibility = count == 1 ? 1 : 0;

                DropdownVisibility = Model.LoadedParams.CurrentWorkspaceModel.CurrentSelection.First().NodeType == "ExtensionNode" ? 1 : 0;
                UpdateColors();

                
            }
            catch (Exception)
            {
                ResetColorWheel();
                UpdateColors();
            }
        }


        private void ResetColorWheel()
        {
            try
            {
                ColorWheelVisibility = false;
                ColorWheelHeight = 48;
                ColorWheelMargin = new Thickness(-36);

            }
            catch (Exception e)
            {
                Model.DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
        }

        #region ColorBrushes
        //TODO: Make this waaaay nicer.

        private void UpdateColors()
        {
            Globals.MonocleGroupSettings.TryGetValue("Group1", out Settings.GroupSetting color1GroupSetting);
            Color1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color1GroupSetting.GroupColor));

            Globals.MonocleGroupSettings.TryGetValue("Group2", out Settings.GroupSetting color2GroupSetting);
            Color2 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color2GroupSetting.GroupColor));

            Globals.MonocleGroupSettings.TryGetValue("Group3", out Settings.GroupSetting color3GroupSetting);
            Color3 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color3GroupSetting.GroupColor));

            Globals.MonocleGroupSettings.TryGetValue("Group4", out Settings.GroupSetting color4GroupSetting);
            Color4 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color4GroupSetting.GroupColor));

            Globals.MonocleGroupSettings.TryGetValue("Group5", out Settings.GroupSetting color5GroupSetting);
            Color5 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color5GroupSetting.GroupColor));

            Globals.MonocleGroupSettings.TryGetValue("Group6", out Settings.GroupSetting color6GroupSetting);
            Color6 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color6GroupSetting.GroupColor));

            GroupSettings = Globals.MonocleGroupSettings.Values.ToList();
        }

        private Brush _color1;
        public Brush Color1
        {
            get => _color1;
            set { _color1 = value; RaisePropertyChanged(nameof(Color1));}
        }

        private Brush _color2;
        public Brush Color2
        {
            get => _color2;
            set { _color2 = value; RaisePropertyChanged(nameof(Color2)); }
        }

        private Brush _color3;
        public Brush Color3
        {
            get => _color3;
            set { _color3 = value; RaisePropertyChanged(nameof(Color3)); }
        }
        private Brush _color4;
        public Brush Color4
        {
            get => _color4;
            set { _color4 = value; RaisePropertyChanged(nameof(Color4)); }
        }
        private Brush _color5;
        public Brush Color5
        {
            get => _color5;
            set { _color5 = value; RaisePropertyChanged(nameof(Color5)); }
        }
        private Brush _color6;
        public Brush Color6
        {
            get => _color6;
            set { _color6 = value; RaisePropertyChanged(nameof(Color6)); }
        }
        private List<Settings.GroupSetting> _groupSettings;
        public List<Settings.GroupSetting> GroupSettings
        {
            get => _groupSettings;
            set { _groupSettings = value; RaisePropertyChanged(nameof(GroupSettings)); }
        }


        #endregion
    }


}
