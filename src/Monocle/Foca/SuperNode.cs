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
                        // Anything else only has a width if it is positionable at all. Zero keeps
                        // the alignment maths defined for things that are not.
                        return Object is ModelBase model ? model.Width : 0;
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
                        return Object is ModelBase model ? model.Height : 0;
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
        // SuperNode is an in-memory wrapper only; it is never persisted to a .dyn.
        protected override void SerializeCore(XmlElement element, SaveContext context)
        {
            throw new NotSupportedException($"{nameof(SuperNode)} is not serializable.");
        }

        protected override void DeserializeCore(XmlElement nodeElement, SaveContext context)
        {
            throw new NotSupportedException($"{nameof(SuperNode)} is not serializable.");
        }
    }
}
