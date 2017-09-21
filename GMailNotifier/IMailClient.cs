// Copyright (C) 2012-2014, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System;
using System.Collections.ObjectModel;
using System.Net;

namespace GMailNotifier
{
    public interface IMailClient : IDisposable
    {
        string MailboxAddress { get; }
        object Icon { get; }
        ObservableCollection<MailMessage> NewMessages { get; set; }

        void SetCredentials( NetworkCredential credentials );
        MailState CheckMailbox();
    }
}