using System;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.ViewModels;

namespace MonocleViewExtension.Foca
{
    public class SuperNode
    {
        public string ObjectType => this.Object.GetType().ToString();
        public object Object { get; set; }
        private double _x;
        private double _y;
        private double _centerX;
        private double _centerY;
        public string GUID { get; set; }

        private double _width;
        private double _height;


        public double Width
        {
            get
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        return group.Width;
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        return note.Width;
                    case "Dynamo.ViewModels.NodeModel":
                        NodeModel node = this.Object as NodeModel;
                        return node.Width;
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
        public double Height
        {
            get
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        return group.Height;
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        return note.Height;
                    case "Dynamo.ViewModels.NodeModel":
                        NodeModel node = this.Object as NodeModel;
                        return node.Height;
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

        public double X
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
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        note.X = value;
                        _x = value;
                        break;
                    default:
                        NodeModel node = this.Object as NodeModel;
                        node.X = value;
                        _x = value;
                        break;
                }
            }
        }
        public double Y
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
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        note.Y = value;
                        _y = value;
                        break;
                    default:
                        NodeModel node = this.Object as NodeModel;
                        node.Y = value;
                        _y = value;
                        break;
                }
            }
        }
        public double CenterY
        {
            get => this._centerY;
            set
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        double ogGroupLocation = group.AnnotationModel.CenterY;
                        group.AnnotationModel.CenterY = value;
                        double translation = ogGroupLocation - (value);
                        foreach (var n in group.Nodes)
                        {
                            n.CenterY = n.CenterY - translation;
                        }

                        _centerY = value;
                        break;
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        note.CenterY = value;
                        _centerY = value;
                        break;
                    default:
                        NodeModel node = this.Object as NodeModel;
                        node.CenterY = value;
                        _centerY = value;
                        break;
                }
            }
        }
        public double CenterX
        {
            get => this._centerX;
            set
            {
                switch (this.ObjectType)
                {
                    case "Dynamo.ViewModels.AnnotationViewModel":
                        AnnotationViewModel group = this.Object as AnnotationViewModel;
                        double ogGroupLocation = group.AnnotationModel.CenterX;
                        group.AnnotationModel.CenterX = value;
                        double translation = ogGroupLocation - (value);
                        foreach (var n in group.Nodes)
                        {
                            n.CenterX = n.CenterX - translation;
                        }
                        _centerX = value;
                        break;
                    case "Dynamo.Graph.Notes.NoteModel":
                        NoteModel note = this.Object as NoteModel;
                        note.CenterX = value;
                        _centerX = value;
                        break;
                    default:
                        NodeModel node = this.Object as NodeModel;
                        node.CenterX = value;
                        _centerX = value;
                        break;
                }
            }
        }
    }


}
