// Copyright (C) 2012, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GMailNotifier.Interop
{
    public sealed class TrayIcon : IDisposable
    {
        #region Private members
        private Guid _guid;
        private IntPtr _ownerHandle;
        private Dictionary<string, string> _resourceTempFiles;
        private string _currentIconPath;
        #endregion

        #region Property-backing fields
        private string _tooltipMessage;
        #endregion

        #region Public properties
        public string TooltipMessage
        {
            get { return _tooltipMessage; }
            set
            {
                _tooltipMessage = value;
                NativeMethods.SetTrayIconTooltip( _guid, _tooltipMessage );
            }
        }

        public Dictionary<MouseEvents, Action> MouseActions { get; private set; }
        #endregion

        public TrayIcon( Window owner )
        {
            _guid = Guid.NewGuid();
            _ownerHandle = new WindowInteropHelper( owner ).EnsureHandle();
            _resourceTempFiles = new Dictionary<string, string>();
            MouseActions = new Dictionary<MouseEvents, Action>();

            HwndSource.FromHwnd( _ownerHandle ).AddHook( MessageHook );
            NativeMethods.CreateTrayIcon( _guid, _ownerHandle );

            // Clean up properly
            WeakEventManager<Application, ExitEventArgs>.AddHandler( Application.Current, "Exit", AppExit );
        }

        public void SetIconPath( string path )
        {
            if ( path != _currentIconPath )
            {
                NativeMethods.SetTrayIconImage( _guid, path );
                _currentIconPath = path;
            }
        }

        public void SetIconAsResource( string resourceName )
        {
            if ( _resourceTempFiles.ContainsKey( resourceName ) && !File.Exists( _resourceTempFiles[resourceName] ) )
            {
                _resourceTempFiles.Remove( resourceName );
            }

            if ( !_resourceTempFiles.ContainsKey( resourceName ) )
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName );
                var bytes = new byte[stream.Length];
                stream.Read( bytes, 0, bytes.Length );

                var tempPath = Path.GetTempFileName();
                File.WriteAllBytes( tempPath, bytes );

                _resourceTempFiles.Add( resourceName, tempPath );
            }

            SetIconPath( _resourceTempFiles[resourceName] );
        }

        #region "Event" handlers
        private IntPtr MessageHook( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
        {
            if ( msg == NativeMethods.MessageId )
            {
                if ( !handled && MouseActions.ContainsKey( (MouseEvents) lParam ) )
                {
                    MouseActions[(MouseEvents) lParam]();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void AppExit( object sender, ExitEventArgs e )
        {
            Dispose();
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            NativeMethods.DeleteTrayIcon( _guid );

            foreach ( string file in _resourceTempFiles.Values )
            {
                if ( File.Exists( file ) )
                {
                    File.Delete( file );
                }
            }
        }
        #endregion

        private static class NativeMethods
        {
            public const int MessageId = 0x8001;

            private const int IconImage = 1;
            private const uint LoadFromFile = 16;

            [DllImport( "shell32" )]
            private static extern bool Shell_NotifyIcon( NotifyCommand cmd, ref NotifyIconData data );

            [DllImport( "user32" )]
            private static extern IntPtr LoadImage( IntPtr instanceHandle, string name, uint type, int sizeX, int sizeY, uint flags );

            private enum NotifyCommand
            {
                Add = 0,
                Modify = 1,
                Delete = 2,
                SetFocus = 3,
                SetVersion = 4
            }

            private enum IconDataFlags
            {
                Callback = 1,
                Icon = 2,
                Tooltip = 4,
                Guid = 32
            }

            private enum IconVisibility
            {
                Visible = 0,
                Hidden = 1,
            }

            [StructLayout( LayoutKind.Sequential )]
            private struct NotifyIconData
            {
                public uint Size;
                public IntPtr WindowHandle;
                public uint TaskbarIconId;
                public IconDataFlags ValidMembers;
                public uint CallbackMessageId;
                public IntPtr IconHandle;
                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 128 )]
                public string ToolTipText;
                public IconVisibility IconState;
                public IconVisibility StateMask;
                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 256 )]
                public string BalloonText;
                public uint Version;
                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 64 )]
                public string BalloonTitle;
                public int BalloonFlags;
                public Guid Guid;
                public IntPtr CustomBalloonIconHandle;
            }

            public static void CreateTrayIcon( Guid iconGuid, IntPtr windowHandle )
            {
                var data = new NotifyIconData
                {
                    Size = (uint) Marshal.SizeOf( typeof( NotifyIconData ) ),
                    CallbackMessageId = MessageId,
                    WindowHandle = windowHandle,
                    Guid = iconGuid,
                    ValidMembers = IconDataFlags.Callback | IconDataFlags.Guid
                };

                Shell_NotifyIcon( NotifyCommand.Add, ref data );
            }

            public static void SetTrayIconTooltip( Guid iconGuid, string message )
            {
                var command = NotifyCommand.Modify;

                var data = new NotifyIconData();
                data.Size = (uint) Marshal.SizeOf( data );
                data.ToolTipText = message;
                data.Guid = iconGuid;
                data.ValidMembers = IconDataFlags.Tooltip | IconDataFlags.Guid;


                Shell_NotifyIcon( command, ref data );
            }

            public static void SetTrayIconImage( Guid iconGuid, string iconPath )
            {
                var iconHandle = LoadImage( IntPtr.Zero, iconPath, IconImage, 0, 0, LoadFromFile );

                var command = NotifyCommand.Modify;

                var data = new NotifyIconData();
                data.Size = (uint) Marshal.SizeOf( data );
                data.IconHandle = iconHandle;
                data.Guid = iconGuid;
                data.ValidMembers = IconDataFlags.Icon | IconDataFlags.Guid;

                Shell_NotifyIcon( command, ref data );
            }

            public static void DeleteTrayIcon( Guid iconGuid )
            {
                var command = NotifyCommand.Delete;
                var data = new NotifyIconData();
                data.Size = (uint) Marshal.SizeOf( data );
                data.Guid = iconGuid;
                data.ValidMembers = IconDataFlags.Guid;

                Shell_NotifyIcon( command, ref data );
            }
        }
    }
}