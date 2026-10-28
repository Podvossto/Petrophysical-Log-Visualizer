using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>
    /// The Explorer-style "Select folder" dialog (address bar, quick access, recent folders) through the Windows shell API.
    /// .NET Framework only offers the old tree-view FolderBrowserDialog; that one is used if the shell dialog cannot be created.
    /// </summary>
    public static class FolderPicker
    {
        private const uint PickFolders = 0x20, ForceFileSystem = 0x40;
        private const uint FileSystemPath = 0x80058000;
        private const int Cancelled = unchecked((int)0x800704C7);

        /// <summary>The chosen folder, or null when the user cancelled.</summary>
        public static string Pick(IWin32Window owner, string title, string startFolder)
        {
            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                try
                {
                    dialog.SetOptions(PickFolders | ForceFileSystem);
                    dialog.SetTitle(title);
                    IShellItem start;
                    if (!string.IsNullOrEmpty(startFolder) && SHCreateItemFromParsingName(startFolder, IntPtr.Zero, typeof(IShellItem).GUID, out start) == 0)
                        dialog.SetFolder(start);
                    int hr = dialog.Show(owner == null ? IntPtr.Zero : owner.Handle);
                    if (hr == Cancelled) return null;
                    Marshal.ThrowExceptionForHR(hr);
                    IShellItem item;
                    dialog.GetResult(out item);
                    IntPtr name;
                    item.GetDisplayName(FileSystemPath, out name);
                    try { return Marshal.PtrToStringUni(name); }
                    finally { Marshal.FreeCoTaskMem(name); }
                }
                finally { Marshal.ReleaseComObject(dialog); }
            }
            catch (Exception)
            {
                using (var old = new FolderBrowserDialog { Description = title, SelectedPath = startFolder ?? "" })
                    return old.ShowDialog(owner) == DialogResult.OK ? old.SelectedPath : null;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem item);

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        // Only the methods up to GetResult are declared; their order must match the COM vtable.
        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(uint count, IntPtr specs);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem item);
            void SetFolder(IShellItem item);
            void GetFolder(out IShellItem item);
            void GetCurrentSelection(out IShellItem item);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem item);
        }

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);
            void GetParent(out IShellItem parent);
            void GetDisplayName(uint form, out IntPtr name);
        }
    }
}
