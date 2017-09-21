// Copyright (C) 2012-2013, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GMailNotifier.Interop;

namespace GMailNotifier
{
    public sealed class Notifier : IDisposable
    {
        #region Constants
        private const int PollInterval = 10000;

        private const string NewMailIcon = "GMailNotifier.Resources.NewMail.ico";
        private const string NoNewMailIcon = "GMailNotifier.Resources.NoNewMail.ico";
        private const string ErrorIcon = "GMailNotifier.Resources.Error.ico";
        #endregion

        #region Private members
        private IMailClient _client;
        private ToastWindow _toast;
        private TrayIcon _trayIcon;
        // used to check if the messages have changed
        private long _lastHash = -1;
        #endregion

        public Notifier()
        {
            _client = new GMailClient();
            SetClientCredentials( false );

            _toast = new ToastWindow();
            _toast.Messages = _client.NewMessages;
            _toast.MailboxAddress = _client.MailboxAddress;

            _trayIcon = new TrayIcon( _toast );
            _trayIcon.MouseActions[MouseEvents.LeftButtonDown] = TrayIconLeftClick;
            _trayIcon.MouseActions[MouseEvents.MiddleButtonDown] = TrayIconMiddleClick;

            SetState( MailState.NoNewMail );
        }

        #region Public methods
        public void Start()
        {
            PollClient();
        }
        #endregion

        #region Private methods
        private void SetClientCredentials( bool force )
        {
            _client.SetCredentials( Authenticator.GetUsernameAndPassword( force ) );
        }

        private async void PollClient()
        {
            while ( true )
            {
                var state = _client.CheckMailbox();

                if ( state == MailState.InvalidCredentials )
                {
                    SetClientCredentials( true );
                }
                else
                {
                    SetTrayTooltip( state );
                    SetState( state );

                    await Task.Delay( PollInterval );
                }
            }
        }

        private bool HaveMessagesChanged()
        {
            // long required to avoid overflows
            long hash = _client.NewMessages.Sum( m => (long) m.GetHashCode() );
            if ( hash == _lastHash )
            {
                return false;
            }

            _lastHash = hash;
            return true;
        }

        private void SetState( MailState state )
        {
            switch ( state )
            {
                case MailState.NewMail:
                    _trayIcon.SetIconAsResource( NewMailIcon );
                    if ( HaveMessagesChanged() )
                    {
                        ShowToast();
                    }
                    break;

                case MailState.NoNewMail:
                    if ( HaveMessagesChanged() )
                    {
                        _trayIcon.SetIconAsResource( NoNewMailIcon );
                        _toast.Hide();
                    }
                    break;

                case MailState.Unknown:
                    if ( HaveMessagesChanged() )
                    {
                        _trayIcon.SetIconAsResource( ErrorIcon );
                        _toast.Hide();
                    }
                    break;
            }
        }

        private async void ShowToast()
        {
            PositionToast();
            _toast.SelectedIndex = 0;
            _toast.Show();

            await Task.Delay( PollInterval );

            _toast.Hide();
        }

        private void PositionToast()
        {
            // Let's pretend we are a tray message
            if ( SystemParameters.WorkArea.Top != 0 )
            {
                // Taskbar on top
                _toast.Left = SystemParameters.WorkArea.Right - _toast.Width;
                _toast.Top = SystemParameters.WorkArea.Top;
            }
            else if ( SystemParameters.WorkArea.Left != 0 )
            {
                // Taskbar to the left
                _toast.Left = SystemParameters.WorkArea.Left;
                _toast.Top = SystemParameters.WorkArea.Bottom - _toast.Height;
            }
            else
            {
                // Taskbar to the right or to the bottom
                _toast.Left = SystemParameters.WorkArea.Right - _toast.Width;
                _toast.Top = SystemParameters.WorkArea.Bottom - _toast.Height;
            }
        }

        private void SetTrayTooltip( MailState state )
        {
            _trayIcon.TooltipMessage = state == MailState.Unknown ? Strings.Error
                                          : _client.NewMessages.Count == 0 ? Strings.NoNewMail
                                          : _client.NewMessages.Count == 1 ? Strings.SingleNewMail
                                          : string.Format( Strings.MultipleNewMail, _client.NewMessages.Count );

            _trayIcon.TooltipMessage += Environment.NewLine + Strings.TooltipEnd;
        }
        #endregion

        #region "Event" handler
        private void TrayIconLeftClick()
        {
            Process.Start( _client.MailboxAddress );
        }

        private void TrayIconMiddleClick()
        {
            App.Current.Shutdown();
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            _client.Dispose();
        }
        #endregion
    }
}