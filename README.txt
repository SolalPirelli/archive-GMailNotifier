GMail notifier I built a long time ago. May still be useful?

---

This is a simple notifier for GMail which uses a tray icon and a toast window to show new messages.
Passwords are stored using Windows's built-in password vault, there is no config file.
Click on the tray icon or on the mail icon in the toast window to open your mailbox ; middle-click on the tray icon to close the app.


Developers:
This app is written in C# using WPF.
The implementations of TrayIcon (which uses Win32's Shell_NotifyIcon) and AuthenticationDialog (which uses CredUIPromptForWindowsCredentials, CredRead, CredWrite and other functions) in GMailNotifier\Interop may be of interest since they use pure Win32 interop and do not rely on the old System.Windows.Forms or System.Drawing.
