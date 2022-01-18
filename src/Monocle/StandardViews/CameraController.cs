using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf.SharpDX;

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
        public static void SetCameraView(Viewport3DX viewPort, eCameraViews view, double animationTime)
        {
            Vector3D faceNormal = GetNormal(view);
            Vector3D faceUp = GetUpVector(view);
            Vector3D lookDirection = -faceNormal;
            Vector3D upDirection = faceUp;
            lookDirection.Normalize();
            upDirection.Normalize();
            HelixToolkit.Wpf.SharpDX.ProjectionCamera camera = viewPort.Camera as HelixToolkit.Wpf.SharpDX.ProjectionCamera;
            if (camera != null)
            {
                Point3D target = camera.Position + camera.LookDirection;
                double distance = camera.LookDirection.Length;
                lookDirection *= distance;
                Point3D newPosition = target - lookDirection;
                viewPort.SetView(newPosition, lookDirection, upDirection, animationTime);
            }
        }
    }
}
