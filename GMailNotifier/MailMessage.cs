// Copyright (C) 2012-2014, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

namespace GMailNotifier
{
    public sealed class MailMessage
    {
        public string Title { get; private set; }

        public string Sender { get; private set; }

        public MailMessage( string title, string sender )
        {
            Title = title;
            Sender = sender;
        }

        public override bool Equals( object obj )
        {
            var msg = obj as MailMessage;
            return msg != null && msg.Sender == this.Sender && msg.Title == this.Title;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Title.GetHashCode();
            hash = hash * 23 + Sender.GetHashCode();
            return hash;
        }
    }
}