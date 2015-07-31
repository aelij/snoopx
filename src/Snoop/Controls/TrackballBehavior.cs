// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Snoop.Controls
{
	public class TrackballBehavior
	{
		public TrackballBehavior(Viewport3D viewport, Point3D lookAtPoint)
		{
			if (viewport == null)
			{
				throw new ArgumentNullException(nameof(viewport));
			}

			_viewport = viewport;
			_lookAtPoint = lookAtPoint;

			var projectionCamera = _viewport.Camera as ProjectionCamera;
			if (projectionCamera != null)
			{
				var offset = projectionCamera.Position - _lookAtPoint;
				_distance = offset.Length;
			}

			viewport.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
			viewport.MouseMove += Viewport_MouseMove;
			viewport.MouseLeftButtonUp += Viewport_MouseLeftButtonUp;
			viewport.MouseWheel += Viewport_MouseWheel;
		}

		public void Reset()
		{
			_orientation = Quaternion.Identity;
			_zoom = 1;
			UpdateCamera();
		}

		private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.MouseDevice.Capture(_viewport);
			var point = e.MouseDevice.GetPosition(_viewport);
			_mouseDirection = GetDirectionFromPoint(point, _viewport.RenderSize);
			_isRotating = true;
			e.Handled = true;
		}
		private void Viewport_MouseMove(object sender, MouseEventArgs e)
		{
			if (_isRotating)
			{
				var point = e.MouseDevice.GetPosition(_viewport);
				var newMouseDirection = GetDirectionFromPoint(point, _viewport.RenderSize);
				var q = GetRotationFromStartAndEnd(newMouseDirection, _mouseDirection, 2);
				_orientation *= q;
				_mouseDirection = newMouseDirection;

				UpdateCamera();
				e.Handled = true;
			}
		}
		private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_isRotating = false;
			e.MouseDevice.Capture(null);
			e.Handled = true;
		}
		private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			// Zoom in or out exponentially.
			_zoom *= Math.Pow(ZoomFactor, e.Delta / 120.0);
			UpdateCamera();
			e.Handled = true;
		}

		private void UpdateCamera()
		{
			var projectionCamera = _viewport.Camera as ProjectionCamera;
			if (projectionCamera != null)
			{
				var matrix = Matrix3D.Identity;
				matrix.Rotate(_orientation);
				projectionCamera.LookDirection = new Vector3D(0, 0, 1) * matrix;
				projectionCamera.UpDirection = new Vector3D(0, -1, 0) * matrix;
				projectionCamera.Position = _lookAtPoint - _distance / _zoom * projectionCamera.LookDirection;
			}
		}

		private static Vector3D GetDirectionFromPoint(Point point, Size size)
		{
			var rx = size.Width / 2;
			var ry = size.Height / 2;
			var r = Math.Min(rx, ry);
			var dx = (point.X - rx) / r;
			var dy = (point.Y - ry) / r;
			var rSquared = dx * dx + dy * dy;
			if (rSquared <= 1)
			{
				return new Vector3D(dx, dy, -Math.Sqrt(2 - rSquared));
			}
		    return new Vector3D(dx, dy, -1 / Math.Sqrt(rSquared));
		}

		private static Quaternion GetRotationFromStartAndEnd(Vector3D start, Vector3D end, double angleMultiplier)
		{
			var factor = start.Length * end.Length;

			if (factor < 1e-6)
			{
				// One or both of the input directions is close to zero in length.
				return Quaternion.Identity;
			}
		    // Both input directions have nonzero length.
		    var axis = Vector3D.CrossProduct(start, end);
		    var dotProduct = Vector3D.DotProduct(start, end) / factor;
		    var angle = Math.Acos(dotProduct < -1 ? -1 : dotProduct > 1 ? 1 : dotProduct);

		    if (axis.LengthSquared < 1e-12)
		    {
		        if (dotProduct > 0)
		        {
		            // The input directions are parallel, so no rotation is needed.
		            return Quaternion.Identity;
		        }
		        // The directions are antiparallel, and therefore a rotation
		        // of 180 degrees about any direction perpendicular to 'start'
		        // (or 'end') will rotate 'start' into 'end'.
		        //
		        // The following construction will guarantee that
		        // dot(axis, start) == 0.
		        //
		        axis = Vector3D.CrossProduct(start, new Vector3D(1, 0, 0));
		        if (axis.LengthSquared < 1e-12)
		        {
		            axis = Vector3D.CrossProduct(start, new Vector3D(0, 1, 0));
		        }
		    }
		    return new Quaternion(axis, angleMultiplier * angle * 180 / Math.PI);
		}

		private readonly Viewport3D _viewport;
		private readonly Point3D _lookAtPoint;
		private readonly double _distance = 1;
		private double _zoom = 1;
		private bool _isRotating;
		private Vector3D _mouseDirection;
		private Quaternion _orientation = Quaternion.Identity;

		private const double ZoomFactor = 1.1;
	}
}
