// Copyright (C) 2012-2014, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GMailNotifier
{
    public sealed partial class ToastWindow : Window, INotifyPropertyChanged
    {
        #region Property-backing fields
        private ObservableCollection<MailMessage> _messages;
        private int _selectedIndex;
        #endregion

        #region Public properties
        public ObservableCollection<MailMessage> Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                FirePropertyChanged();
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                FirePropertyChanged();
            }
        }

        public string MailboxAddress { get; set; }
        #endregion

        public ToastWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        public ICommand OpenMailboxCommand
        {
            get { return new RelayCommand( _ => Process.Start( MailboxAddress ) ); }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged( [CallerMemberName] string propertyName = "" )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
        #endregion
    }
}