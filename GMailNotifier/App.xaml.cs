// Copyright (C) 2012, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System.Windows;

namespace GMailNotifier
{
    public partial class App : Application 
    {
        private Notifier _notifier;

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );
            _notifier = new Notifier();
            _notifier.Start();
        }

        protected override void OnExit( ExitEventArgs e )
        {
            base.OnExit( e );
            _notifier.Dispose();
        }
    }
}