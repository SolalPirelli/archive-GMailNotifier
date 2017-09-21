// Copyright (C) 2012-2014, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using ImapX;
using ImapX.Authentication;

namespace GMailNotifier
{
    public sealed class GMailClient : IMailClient
    {
        #region Constants
        private const string ServerUrl = "imap.gmail.com";
        private const int ServerPort = 993;
        private const bool ServerUsesSsl = true;
        #endregion

        #region Private members
        private ImapClient _client;
        #endregion

        #region Public properties
        public string MailboxAddress
        {
            get { return "http://www.gmail.com"; }
        }

        public object Icon
        {
            get { return Application.Current.Resources["GMailIcon"]; }
        }

        public ObservableCollection<MailMessage> NewMessages { get; set; }
        #endregion

        public GMailClient()
        {
            NewMessages = new ObservableCollection<MailMessage>();
            _client = new ImapClient( ServerUrl, ServerPort, useSsl: ServerUsesSsl, validateServerCertificate: true );
        }

        #region Public methods
        public void SetCredentials( NetworkCredential credentials )
        {
            _client.Credentials = new PlainCredentials( credentials.UserName, credentials.Password );
        }

        public MailState CheckMailbox()
        {
            try
            {
                bool connOk = _client.Connect();
                if ( !connOk )
                {
                    return MailState.Unknown;
                }

                bool credOk = _client.Login();
                if ( !credOk )
                {
                    return MailState.InvalidCredentials;
                }

                try
                {
                    var newMessages = new HashSet<MailMessage>();

                    foreach ( var folder in _client.Folders )
                    {
                        if ( folder.Selectable )
                        {
                            var unreadMessages = folder.Search( "UNSEEN" );

                            foreach ( var message in unreadMessages )
                            {
                                string from = string.IsNullOrEmpty( message.From.DisplayName ) ? message.From.Address : message.From.DisplayName;
                                newMessages.Add( new MailMessage( from, message.Subject ) );
                            }
                        }
                    }

                    NewMessages.Clear();
                    foreach ( var message in newMessages )
                    {
                        NewMessages.Add( message );
                    }

                    return NewMessages.Count == 0 ? MailState.NoNewMail : MailState.NewMail;
                }
                catch
                {
                    return MailState.Unknown;
                }
            }
            finally
            {
                _client.Disconnect();
            }
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