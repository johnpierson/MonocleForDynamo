using System;
using System.Xml;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.ViewModels;
#pragma warning disable CS0618 // Type or member is obsolete

namespace MonocleViewExtension.Foca
{
    public class SuperNode : ModelBase
    {
        public object Object { get; set; }
        public string ObjectType => this.Object.GetType().ToString();
        public string Guid { get; set; }
        public DynamoViewModel Dvm { get; set; }
        private double _x;
        private double _y;
        //private double _centerX;
        //private double _centerY;
        //private double _width;
        //private double _height;

        
        public override double Width
        {
            get
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        return group.Width;
                    case "Dynamo.ViewModels.NoteViewModel":
                        NoteViewModel note = this.Object as NoteViewModel;
                        return note.Model.Width;
                    case "Dynamo.ViewModels.NodeViewModel":
                        NodeViewModel node = this.Object as NodeViewModel;
                        return node.NodeModel.Width;
                    default:
                        try
                        {
                            return ((ModelBase)Object).Width;
                        }
                        catch (Exception)
                        {
                            return 0;
                        }
                }
            }
        }
        public override double Height
        {
            get
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        return group.Height;
                    case "Dynamo.ViewModels.NoteViewModel":
                        NoteViewModel note = this.Object as NoteViewModel;
                        return note.Model.Height;
                    case "Dynamo.ViewModels.NodeViewModel":
                        NodeViewModel node = this.Object as NodeViewModel;
                        return node.NodeModel.Height;
                    default:
                        try
                        {
                            return ((ModelBase)Object).Height;
                        }
                        catch (Exception)
                        {
                            return 0;
                        }
                }
            }
        }

        public new double X
        {
            get => this._x;
            set
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        double ogGroupLocation = group.AnnotationModel.X;
                        group.AnnotationModel.X = value;
                        double translation = ogGroupLocation - (value);
                        foreach (var n in group.Nodes)
                        {
                            n.X = n.X - translation;
                        }
                        _x = value;
                        break;
                    case "Dynamo.ViewModels.NoteViewModel":
                        NoteViewModel note = this.Object as NoteViewModel;
                        note.Left = value;
                        _x = value;
                        break;
                    default:
                        NodeViewModel node = this.Object as NodeViewModel;
                        node.X = value;
                        _x = value;
                        break;
                }
            }
        }
        public new double Y
        {
            get => this._y;
            set
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        double buffer = 0;//was 5
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        double ogGroupLocation = group.AnnotationModel.Y;
                        group.AnnotationModel.Y = value + buffer;
                        double translation = ogGroupLocation - (value + buffer);
                        foreach (var n in group.Nodes)
                        {
                            n.Y = n.Y - translation;
                        }
                        _y = value + buffer;
                        break;
                    case "Dynamo.ViewModels.NoteViewModel":
                        NoteViewModel note = this.Object as NoteViewModel;
                        note.Top = (value + (note.Model.Height/2));
                        _y = value;
                        break;
                    default:
                        NodeViewModel node = this.Object as NodeViewModel;
                        node.Y = value;
                        _y = value;
                        break;
                }
            }
        }
        //public new double CenterY
        //{
        //    get => this._centerY;
        //    set
        //    {
        //        switch (this.ObjectType)
        //        {
        //            case "Dynamo.ViewModels.AnnotationViewModel":
        //                AnnotationViewModel group = this.Object as AnnotationViewModel;
        //                double ogGroupLocation = group.AnnotationModel.CenterY;
        //                group.AnnotationModel.CenterY = value;
        //                double translation = ogGroupLocation - (value);
        //                foreach (var n in group.Nodes)
        //                {
        //                    n.CenterY = n.CenterY - translation;
        //                }

        //                _centerY = value;
        //                break;
        //            case "Dynamo.Graph.Notes.NoteModel":
        //                NoteModel note = this.Object as NoteModel;
        //                note.CenterY = value;
        //                _centerY = value;
        //                break;
        //            default:
        //                NodeModel node = this.Object as NodeModel;
        //                node.CenterY = value;
        //                _centerY = value;
        //                break;
        //        }
        //    }
        //}

        protected override void SerializeCore(XmlElement element, SaveContext context)
        {
            throw new NotImplementedException();
        }

        protected override void DeserializeCore(XmlElement nodeElement, SaveContext context)
        {
            throw new NotImplementedException();
        }

        //public new double CenterX
        //{
        //    get => this._centerX;
        //    set
        //    {
        //        switch (this.ObjectType)
        //        {
        //            case "Dynamo.ViewModels.AnnotationViewModel":
        //                AnnotationViewModel group = this.Object as AnnotationViewModel;
        //                double ogGroupLocation = group.AnnotationModel.CenterX;
        //                group.AnnotationModel.CenterX = value;
        //                double translation = ogGroupLocation - (value);
        //                foreach (var n in group.Nodes)
        //                {
        //                    n.CenterX = n.CenterX - translation;
        //                }
        //                _centerX = value;
        //                break;
        //            case "Dynamo.Graph.Notes.NoteModel":
        //                NoteModel note = this.Object as NoteModel;
        //                note.CenterX = value;
        //                _centerX = value;
        //                break;
        //            default:
        //                NodeModel node = this.Object as NodeModel;
        //                node.CenterX = value;
        //                _centerX = value;
        //                break;
        //        }
        //    }
        //}
    }


}
