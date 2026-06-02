using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Dynamo.Models;
using Dynamo.ViewModels;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.GraphInformation
{
    internal class GraphInformationModel
    {
        private const double GroupPadding = 24;
        private const double HeaderHeight = 80;
        private const double NoteWidth = 320;
        private const double SmallNoteHeight = 90;
        private const double DescriptionNoteHeight = 150;
        private const double ColumnSpacing = 24;
        private const double RowSpacing = 24;
        private const double InsertRightOffset = 200;
        private const double GroupFontSize = 48;
        private const string GroupTitle = "Graph Information";
        private const string GroupSubtitle = "metadata, color code legend, naming rules";

        private readonly DynamoViewModel _dynamoViewModel;

        public GraphInformationModel(DynamoViewModel dvm)
        {
            _dynamoViewModel = dvm;
        }

        public void CreateGraphInformationTemplate()
        {
            var basePoint = GetInsertionOrigin();
            var groupRect = GetGroupRect(basePoint);

            var group = CreateGroup(groupRect);
            if (group == null) return;

            var notes = BuildNoteDefinitions(basePoint);
            foreach (var note in notes)
            {
                var noteGuid = CreateNote(note);
                AddNoteToGroup(group, noteGuid);
            }
        }

        private Point GetInsertionOrigin()
        {
            var bounds = GetWorkspaceBounds();
            if (bounds == null) return new Point(0, 0);

            return new Point(bounds.Value.Right + InsertRightOffset, bounds.Value.Top);
        }

        private Rect GetGroupRect(Point basePoint)
        {
            var groupWidth = (NoteWidth * 2) + ColumnSpacing + (GroupPadding * 2);
            var groupHeight = HeaderHeight + (SmallNoteHeight * 3) + (RowSpacing * 3) + DescriptionNoteHeight + GroupPadding;

            return new Rect(basePoint.X, basePoint.Y, groupWidth, groupHeight);
        }

        private AnnotationViewModel CreateGroup(Rect groupRect)
        {
            var centerX = groupRect.X + (groupRect.Width / 2);
            var centerY = groupRect.Y + (groupRect.Height / 2);

#if net8 || net10
            var annotationCommand = new DynamoModel.CreateAnnotationCommand(
                Guid.NewGuid(),
                GroupTitle,
                GroupSubtitle,
                centerX,
                centerY,
                false);
#endif
#if !net8 && !net10
            var annotationCommand = new DynamoModel.CreateAnnotationCommand(
                Guid.NewGuid(),
                $"{GroupTitle}{Environment.NewLine}{GroupSubtitle}",
                centerX,
                centerY,
                false);
#endif
            _dynamoViewModel.Model.ExecuteCommand(annotationCommand);

            var group = _dynamoViewModel.CurrentSpaceViewModel.Annotations.LastOrDefault();
            if (group == null) return null;

            group.Background = (Color)ColorConverter.ConvertFromString("#BFBFBF");
            group.FontSize = GroupFontSize;
            return group;
        }

        private Guid CreateNote(NoteDefinition note)
        {
            var noteGuid = Guid.NewGuid();
            var noteCommand = new DynamoModel.CreateNoteCommand(
                noteGuid,
                note.Text,
                note.X,
                note.Y,
                false);

            _dynamoViewModel.Model.ExecuteCommand(noteCommand);

            var noteVm = _dynamoViewModel.CurrentSpaceViewModel.Notes.FirstOrDefault(n => n.Model.GUID == noteGuid);
            if (noteVm != null)
            {
                noteVm.Model.Width = note.Width;
                noteVm.Model.Height = note.Height;
            }

            return noteGuid;
        }

        private void AddNoteToGroup(AnnotationViewModel group, Guid noteGuid)
        {
            group.AnnotationModel.Select();
            _dynamoViewModel.Model.ExecuteCommand(new DynamoModel.AddModelToGroupCommand(noteGuid.ToString()));
            group.AnnotationModel.Deselect();
        }

        private List<NoteDefinition> BuildNoteDefinitions(Point basePoint)
        {
            var fileName = GetCurrentGraphName();
            var author = Environment.UserName;
            var dateStamp = DateTime.Now.ToString("MMM. dd yyyy");
            var dynamoVersion = Globals.DynamoVersion != null ? Globals.DynamoVersion.ToString() : "N/A";

            var leftX = basePoint.X + GroupPadding;
            var rightX = leftX + NoteWidth + ColumnSpacing;

            var row1Y = basePoint.Y + HeaderHeight;
            var row2Y = row1Y + SmallNoteHeight + RowSpacing;
            var row3Y = row2Y + SmallNoteHeight + RowSpacing;
            var row4Y = row3Y + SmallNoteHeight + RowSpacing;

            return new List<NoteDefinition>
            {
                new NoteDefinition(leftX, row1Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Dynamo Project", fileName)),
                new NoteDefinition(rightX, row1Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Dynamo Version", dynamoVersion)),
                new NoteDefinition(leftX, row2Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Associated File(s)", "N/A")),
                new NoteDefinition(rightX, row2Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Host Application Version", "N/A")),
                new NoteDefinition(leftX, row3Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Author(s) / Creation Date", $"{author} / {dateStamp}")),
                new NoteDefinition(rightX, row3Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Required Dynamo Packages", "N/A")),
                new NoteDefinition(leftX, row4Y, NoteWidth, DescriptionNoteHeight,
                    BuildNoteText("Description","description goes here")),
                new NoteDefinition(rightX, row4Y, NoteWidth, SmallNoteHeight,
                    BuildNoteText("Additional Comments", "N/A"))
            };
        }

        private string GetCurrentGraphName()
        {
            var fileName = _dynamoViewModel.CurrentSpaceViewModel.FileName;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return System.IO.Path.GetFileName(fileName);
            }

            return _dynamoViewModel.CurrentSpaceViewModel.Name;
        }

        private static string BuildNoteText(string title, string body)
        {
            var divider = new string('-', 30);
            return $"{title.ToUpperInvariant()}{Environment.NewLine}{divider}{Environment.NewLine}{body}";
        }

        private Rect? GetWorkspaceBounds()
        {
            Rect? bounds = null;

            foreach (var node in _dynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                var rect = new Rect(node.X, node.Y, node.NodeModel.Width, node.NodeModel.Height);
                bounds = bounds == null ? rect : Rect.Union(bounds.Value, rect);
            }

            foreach (var note in _dynamoViewModel.CurrentSpaceViewModel.Notes)
            {
                var rect = new Rect(note.Left, note.Top, note.Model.Width, note.Model.Height);
                bounds = bounds == null ? rect : Rect.Union(bounds.Value, rect);
            }

            return bounds;
        }

        private readonly struct NoteDefinition
        {
            public NoteDefinition(double x, double y, double width, double height, string text)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Text = text;
            }

            public double X { get; }
            public double Y { get; }
            public double Width { get; }
            public double Height { get; }
            public string Text { get; }
        }
    }
}
