using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;

namespace MonocleViewExtension.Utilities
{
    public class SmartCursor : IDisposable
    {
        [DllImport("user32.dll")]
        static extern IntPtr CreateIconIndirect([In] ref ICONINFO piconinfo);

        [DllImport("user32.dll")]
        static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO
        {
            /// <summary>
            /// Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies an icon; FALSE specifies a cursor. 
            /// </summary>
            public bool fIcon;
            /// <summary>
            /// Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot spot is always in the center of the icon, and this member is ignored.
            /// </summary>
            public Int32 xHotspot;
            /// <summary>
            /// Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot spot is always in the center of the icon, and this member is ignored. 
            /// </summary>
            public Int32 yHotspot;
            /// <summary>
            /// (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, this bitmask is formatted so that the upper half is the icon AND bitmask and the lower half is the icon XOR bitmask. Under this condition, the height should be an even multiple of two. If this structure defines a color icon, this mask only defines the AND bitmask of the icon. 
            /// </summary>
            public IntPtr hbmMask;
            /// <summary>
            /// (HBITMAP) Handle to the icon color bitmap. This member can be optional if this structure defines a black and white icon. The AND bitmask of hbmMask is applied with the SRCAND flag to the destination; subsequently, the color bitmap is applied (using XOR) to the destination by using the SRCINVERT flag. 
            /// </summary>
            public IntPtr hbmColor;
        }

        public ICONINFO Info
        {
            get; private set;
        }

        public System.Windows.Forms.Cursor FormsCursor
        {
            get; private set;
        }

        public System.Windows.Input.Cursor WpfCursor
        {
            get; private set;
        }

        internal System.Windows.Forms.Cursor GetFormsIcon(Bitmap bmp, int hot_x, int hot_y)
        {
            // Initialize the cursor information.
            IntPtr h_icon = bmp.GetHicon();
            GetIconInfo(h_icon, out var icon_info);
            icon_info.xHotspot = hot_x;
            icon_info.yHotspot = hot_y;
            icon_info.fIcon = false;    // Cursor, not icon.
                                        // Create the cursor.
            IntPtr h_cursor = CreateIconIndirect(ref icon_info);
            return new System.Windows.Forms.Cursor(h_cursor);
        }

        internal System.Windows.Input.Cursor GetWpfIcon(IntPtr i)
        {
            SafeFileHandle Handle = new SafeFileHandle(i, false);
            return System.Windows.Interop.CursorInteropHelper.Create(Handle);
        }

        internal System.Windows.Input.Cursor GetWpfIcon(System.Windows.Forms.Cursor Cursor)
        {
            return GetWpfIcon(Cursor.Handle);
        }

        internal void SetFromBitmap(Bitmap Bitmap, int HotX, int HotY)
        {
            this.FormsCursor = GetFormsIcon(Bitmap, HotX, HotY);
            this.WpfCursor = GetWpfIcon(this.FormsCursor);
        }

        internal void SetFromForms(System.Windows.Forms.Cursor Cursor)
        {
            this.FormsCursor = Cursor;
            this.WpfCursor = GetWpfIcon(this.FormsCursor);
        }

        internal void SetFromWpf(System.Windows.Input.Cursor Cursor)
        {
            this.WpfCursor = Cursor;
        }

        /// <summary>
        /// Creats a new cursor from a Windows Presentation Foundation cursor (System.Windows.Input).
        /// </summary>
        public SmartCursor(System.Windows.Input.Cursor Cursor)
        {
            SetFromWpf(Cursor);
        }

        /// <summary>
        /// Creates a new cursor from a Windows Forms cursor (System.Windows.Forms).
        /// </summary>
        public SmartCursor(System.Windows.Forms.Cursor Cursor)
        {
            SetFromForms(Cursor);
        }

        /// <summary>
        /// Creates a new cursor from bitmap.
        /// </summary>
        public SmartCursor(Bitmap Bitmap, int HotX, int HotY)
        {
            SetFromBitmap(Bitmap, HotX, HotY);
        }

        /// <summary>
        /// Creates a new cursor from a resource in specified assembly.
        /// </summary>
        public SmartCursor(string AssemblyName, string ResourcePath, int HotX, int HotY)
        {

            Bitmap Bitmap = BitmapImage2Bitmap(new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/" + AssemblyName + ";component/" + ResourcePath, UriKind.RelativeOrAbsolute)));
            SetFromBitmap(Bitmap, HotX, HotY);
        }

        public void Dispose()
        {
            if (this.FormsCursor == null) return;
            DestroyIcon(this.FormsCursor.Handle);
            this.FormsCursor.Dispose();
            this.WpfCursor.Dispose();
        }
        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
    }
}