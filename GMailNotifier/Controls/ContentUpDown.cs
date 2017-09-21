// Copyright (C) 2012, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GMailNotifier.Controls
{
    public sealed class ContentUpDown : Selector
    {
        public ICommand UpCommand
        {
            get
            {
                return new RelayCommand( _ => SelectedIndex--,
                                         _ => SelectedIndex > 0 );
            }
        }

        public ICommand DownCommand
        {
            get
            {
                return new RelayCommand( _ => SelectedIndex++,
                                         _ => SelectedIndex < Items.Count - 1 );
            }
        }
    }
}