// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Snoop.Controls
{
    public class VisualTree3DView : Viewport3D
    {
        public VisualTree3DView(Visual visual)
        {
            var directionalLight1 = new DirectionalLight(Colors.White, new Vector3D(0, 0, 1));
            var directionalLight2 = new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));

            double z = 0;
            var model = ConvertVisualToModel3D(visual, ref z);

            var group = new Model3DGroup();
            group.Children.Add(directionalLight1);
            group.Children.Add(directionalLight2);
            group.Children.Add(model);
            _zScaleTransform = new ScaleTransform3D();
            group.Transform = _zScaleTransform;

            var modelVisual = new ModelVisual3D { Content = group };

            var bounds = model.Bounds;
            const double fieldOfView = 45;
            var lookAtPoint = new Point3D(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);
            var cameraDistance = 0.5 * bounds.SizeX / Math.Tan(0.5 * fieldOfView * Math.PI / 180);
            var position = lookAtPoint - new Vector3D(0, 0, cameraDistance);
            Camera camera = new PerspectiveCamera(position, new Vector3D(0, 0, 1), new Vector3D(0, -1, 0), fieldOfView);

            _zScaleTransform.CenterZ = lookAtPoint.Z;

            Children.Add(modelVisual);
            Camera = camera;
            ClipToBounds = false;
            Width = 500;
            Height = 500;

            _trackballBehavior = new TrackballBehavior(this, lookAtPoint);
        }

        public double ZScale
        {
            get { return _zScaleTransform.ScaleZ; }
            set { _zScaleTransform.ScaleZ = value; }
        }

        public void Reset()
        {
            _trackballBehavior.Reset();
            ZScale = 1;
        }

        private Model3D ConvertVisualToModel3D(Visual visual, ref double z)
        {
            Model3D model = null;
            var bounds = VisualTreeHelper.GetContentBounds(visual);
            var viewport = visual as Viewport3D;
            if (viewport != null)
            {
                bounds = new Rect(viewport.RenderSize);
            }
            if (_includeEmptyVisuals)
            {
                bounds.Union(VisualTreeHelper.GetDescendantBounds(visual));
            }
            if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
            {
                var mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(bounds.Left, bounds.Top, z));
                mesh.Positions.Add(new Point3D(bounds.Right, bounds.Top, z));
                mesh.Positions.Add(new Point3D(bounds.Right, bounds.Bottom, z));
                mesh.Positions.Add(new Point3D(bounds.Left, bounds.Bottom, z));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 2, 3, 0 });
                mesh.Freeze();

                var brush = MakeBrushFromVisual(visual, bounds);
                var material = new DiffuseMaterial(brush);
                material.Freeze();

                model = new GeometryModel3D(mesh, material);
                ((GeometryModel3D)model).BackMaterial = material;

                z -= 1;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(visual);
            if (childrenCount > 0)
            {
                var group = new Model3DGroup();
                if (model != null)
                {
                    group.Children.Add(model);
                }
                for (var i = 0; i < childrenCount; i++)
                {
                    var childVisual = VisualTreeHelper.GetChild(visual, i) as Visual;
                    if (childVisual != null)
                    {
                        var childModel = ConvertVisualToModel3D(childVisual, ref z);
                        if (childModel != null)
                        {
                            group.Children.Add(childModel);
                        }
                    }
                }
                model = group;
            }

            if (model != null)
            {
                var transform = VisualTreeHelper.GetTransform(visual);
                var matrix = transform?.Value ?? Matrix.Identity;
                var offset = VisualTreeHelper.GetOffset(visual);
                matrix.Translate(offset.X, offset.Y);
                if (!matrix.IsIdentity)
                {
                    var matrix3D = new Matrix3D(matrix.M11, matrix.M12, 0, 0, matrix.M21, matrix.M22, 0, 0, 0, 0, 1, 0, matrix.OffsetX, matrix.OffsetY, 0, 1);
                    Transform3D transform3D = new MatrixTransform3D(matrix3D);
                    transform3D.Freeze();
                    model.Transform = transform3D;
                }
                model.Freeze();
            }

            return model;
        }
        private Brush MakeBrushFromVisual(Visual visual, Rect bounds)
        {
            var viewport = visual as Viewport3D;
            if (viewport == null)
            {
                Drawing drawing = VisualTreeHelper.GetDrawing(visual);
                if (_drawOutlines)
                {
                    bounds.Inflate(_outlinePen.Thickness / 2, _outlinePen.Thickness / 2);
                }

                var offsetMatrix = new Matrix(1, 0, 0, 1, -bounds.Left, -bounds.Top);
                var offsetMatrixTransform = new MatrixTransform(offsetMatrix);
                offsetMatrixTransform.Freeze();

                var drawingVisual = new DrawingVisual();
                var drawingContext = drawingVisual.RenderOpen();
                drawingContext.PushTransform(offsetMatrixTransform);
                if (_drawOutlines)
                {
                    drawingContext.DrawRectangle(null, _outlinePen, bounds);
                }
                drawingContext.DrawDrawing(drawing);
                drawingContext.Pop();
                drawingContext.Close();

                visual = drawingVisual;
            }

            var renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Default);
            if (viewport != null)
            {
                typeof(RenderTargetBitmap).GetMethod("RenderForBitmapEffect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderTargetBitmap,
                    new object[] { visual, Matrix.Identity, Rect.Empty });
            }
            else
            {
                renderTargetBitmap.Render(visual);
            }
            renderTargetBitmap.Freeze();
            var imageBrush = new ImageBrush(renderTargetBitmap);
            imageBrush.Freeze();

            return imageBrush;
        }

        private readonly bool _drawOutlines = false;
        private readonly bool _includeEmptyVisuals = false;
        private readonly TrackballBehavior _trackballBehavior;
        private readonly ScaleTransform3D _zScaleTransform;

        private static readonly Pen _outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), 2);
    }
}
