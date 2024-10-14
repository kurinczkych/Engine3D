using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public static class FolderDialogShell32
    {
        // Declare the P/Invoke function to call the native folder browser dialog
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public IntPtr iImage;
        }

        // Flags for the folder browser dialog
        private const uint BIF_RETURNONLYFSDIRS = 0x0001;
        private const uint BIF_NEWDIALOGSTYLE = 0x0040;  // Enables the 'new' folder dialog style

        public static string SaveFolderDialog()
        {
            StringBuilder folderPath = new StringBuilder(260);
            BROWSEINFO bi = new BROWSEINFO();
            bi.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;
            bi.lpszTitle = "Select a folder to save your project";

            // Open the folder browser dialog
            IntPtr pidl = SHBrowseForFolder(ref bi);
            if (pidl != IntPtr.Zero)
            {
                // Get the selected folder path
                if (SHGetPathFromIDList(pidl, folderPath))
                {
                   return folderPath.ToString();
                }
            }
            return "";
        }

    }
}
