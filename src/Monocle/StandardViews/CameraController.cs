using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Media3D;


namespace MonocleViewExtension.StandardViews
{
    internal static class CameraController
    {
        public enum eCameraViews
        {
            Front,
            Back,
            Right,
            Left,
            Top,
            Bottom
        }
        private struct CameraViews
        {
            public struct StandardViews
            {
                public struct Normals
                {
                    public static Vector3D Front => new Vector3D(0, 0, 1);

                    public static Vector3D Back => new Vector3D(0, 0, -1);

                    public static Vector3D Right => new Vector3D(1, 0, 0);

                    public static Vector3D Left => new Vector3D(-1, 0, 0);

                    public static Vector3D Top => new Vector3D(0, 1, 0);

                    public static Vector3D Bottom => new Vector3D(0, -1, 0);
                }
                public struct UpVectors
                {
                    public static Vector3D Front => new Vector3D(0, 1, 0);

                    public static Vector3D Back => new Vector3D(0, 1, 0);

                    public static Vector3D Right => new Vector3D(0, 1, 0);

                    public static Vector3D Left => new Vector3D(0, 1, 0);

                    public static Vector3D Top => new Vector3D(0, 0, -1);

                    public static Vector3D Bottom => new Vector3D(0, 0, -1);
                }
            }

        }
        private static Vector3D GetNormal(eCameraViews view)
        {
            switch (view)
            {
                case eCameraViews.Front: return CameraViews.StandardViews.Normals.Front;
                case eCameraViews.Back: return CameraViews.StandardViews.Normals.Back;
                case eCameraViews.Right: return CameraViews.StandardViews.Normals.Right;
                case eCameraViews.Left: return CameraViews.StandardViews.Normals.Left;
                case eCameraViews.Top: return CameraViews.StandardViews.Normals.Top;
                case eCameraViews.Bottom: return CameraViews.StandardViews.Normals.Bottom;
                default:
                    throw new NotSupportedException();
            }
        }
        private static Vector3D GetUpVector(eCameraViews view)
        {
            switch (view)
            {
                case eCameraViews.Front: return CameraViews.StandardViews.UpVectors.Front;
                case eCameraViews.Back: return CameraViews.StandardViews.UpVectors.Back;
                case eCameraViews.Right: return CameraViews.StandardViews.UpVectors.Right;
                case eCameraViews.Left: return CameraViews.StandardViews.UpVectors.Left;
                case eCameraViews.Top: return CameraViews.StandardViews.UpVectors.Top;
                case eCameraViews.Bottom: return CameraViews.StandardViews.UpVectors.Bottom;
                default:
                    throw new NotSupportedException();
            }
        }
        /// <summary>
        /// Animate Dynamo's background preview camera to a standard view.
        /// The viewport is Helix's Viewport3DX, but it is driven through reflection so Monocle has no compile-time
        /// reference to HelixToolkit and always binds to whichever Helix build the host Dynamo ships.
        /// </summary>
        public static bool SetCameraView(FrameworkElement viewPort, eCameraViews view, double animationTime)
        {
            if (viewPort == null) return false;

            var camera = viewPort.GetType().GetProperty("Camera", BindingFlags.Public | BindingFlags.Instance)?.GetValue(viewPort);
            if (camera == null) return false;

            var cameraType = camera.GetType();
            var positionProperty = cameraType.GetProperty("Position");
            var lookProperty = cameraType.GetProperty("LookDirection");
            var upProperty = cameraType.GetProperty("UpDirection");
            if (!(positionProperty?.GetValue(camera) is Point3D position) || !(lookProperty?.GetValue(camera) is Vector3D currentLook))
            {
                return false;
            }

            Vector3D lookDirection = -GetNormal(view);
            Vector3D upDirection = GetUpVector(view);
            lookDirection.Normalize();
            upDirection.Normalize();

            Point3D target = position + currentLook;
            lookDirection *= currentLook.Length;
            Point3D newPosition = target - lookDirection;

            var setView = viewPort.GetType().GetMethod("SetView", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(Point3D), typeof(Vector3D), typeof(Vector3D), typeof(double) }, null);
            if (setView != null)
            {
                setView.Invoke(viewPort, new object[] { newPosition, lookDirection, upDirection, animationTime });
                return true;
            }

            // Fallback for a Helix build without SetView: jump without animation.
            positionProperty.SetValue(camera, newPosition);
            lookProperty.SetValue(camera, lookDirection);
            upProperty?.SetValue(camera, upDirection);
            return true;
        }

        internal static bool IsHelixViewport(FrameworkElement element) =>
            element != null && element.GetType().Name == "Viewport3DX";
    }
}
