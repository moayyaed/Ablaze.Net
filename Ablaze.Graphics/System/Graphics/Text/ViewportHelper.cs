﻿#pragma warning disable 1591
using System.Drawing;
using System.Numerics;
using System.Graphics.OGL;

namespace System.Graphics.Text {
	public static class ViewportHelper {
		//The currently set viewport
		public static RectangleF? CurrentViewport {
			get {

				if (_currentViewport == null) {
					UpdateCurrentViewport();
				}

				return _currentViewport;
			}
		}
		private static RectangleF? _currentViewport;

		public static void UpdateCurrentViewport() {
			int[] viewportInts = new int[4];

			//TODO Test that this function returns the expected values -> tested, 96% sure it works.
			GL.GetInteger(GetPName.Viewport, viewportInts);
			_currentViewport = new RectangleF(viewportInts[0], viewportInts[1], viewportInts[2], viewportInts[3]);
		}

		public static void InvalidateViewport() {
			_currentViewport = null;
		}

		public static bool IsOrthographicProjection(ref Matrix4 mat) {
			return !(mat.M12 != 0 || mat.M13 != 0 || mat.M14 != 0 || mat.M21 != 0 || mat.M23 != 0 || mat.M24 != 0 || mat.M31 != 0 || mat.M32 != 0 || mat.M34 != 0 || mat.M44 != 1f);
		}

		public static RectangleF GetViewportFromOrthographicProjection(ref Matrix4 mat) {
			if (!IsOrthographicProjection(ref mat))
				throw new ArgumentException("Matrix is not an orthographic projection matrix", nameof(mat));

			float left = -(1f + mat.M41) / (mat.M11);
			float right = (1f - mat.M41) / (mat.M11);

			float bottom = -(1 + mat.M42) / (mat.M22);
			float top = (1 - mat.M42) / (mat.M22);

			return new RectangleF(left, bottom, right - left, top - bottom);
		}
	}
}

#pragma warning restore 1591