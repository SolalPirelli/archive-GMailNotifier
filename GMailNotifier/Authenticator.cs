// Copyright (C) 2012, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System.Net;
using GMailNotifier.Interop;

namespace GMailNotifier
{
    public sealed class Authenticator
    {
        private const string ApplicationCredentialId = "GMail Notifier";

        public static NetworkCredential GetUsernameAndPassword( bool forceNew )
        {
            return AuthenticationDialog.GetCredential( ApplicationCredentialId, Strings.AuthenticationCaption, Strings.AuthenticationMessage, forceNew );
        }
    }
}