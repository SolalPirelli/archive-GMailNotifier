// Copyright (C) 2012, Solal Pirelli
// This code is licensed under a modified MIT License (see Properties\Licence.txt for details).
// Redistributions of this source code or compiled versions of it must retain the above copyright notice and the licence.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace GMailNotifier.Interop
{
    public static class AuthenticationDialog
    {
        /// <summary>
        /// Gets credentials using the built-in Windows prompt.
        /// </summary>
        /// <remarks>This method only works on Windows Vista and higher.</remarks>
        public static NetworkCredential GetCredential( string caption, string message )
        {
            bool save = false;
            return NativeMethods.ShowCredentialsPrompt( caption, message, ref save );
        }

        /// <summary>
        /// Gets credentials from the credentials vault if possible, or with a prompt.
        /// The credentials are then saved in the credentials vault if the user wishes to.
        /// </summary>
        /// <param name="forceNew">If true, the credentials will not be read from the vault but asked, regardless of their presence in the vault.</param>
        /// <remarks>This method only works on Windows Vista and higher.</remarks>
        public static NetworkCredential GetCredential( string credentialName, string caption, string message, bool forceNew = false )
        {
            NetworkCredential credentials = null;
            if ( !forceNew )
            {
                credentials = NativeMethods.ReadCredential( credentialName );
            }

            if ( credentials == null )
            {
                bool save = true;
                credentials = NativeMethods.ShowCredentialsPrompt( caption, message, ref save );

                if ( save )
                {
                    NativeMethods.WriteCredential( credentialName, credentials );
                }
                else
                {
                    NativeMethods.DeleteCredential( credentialName );
                }
            }

            return credentials;
        }

        /// <summary>
        /// Deletes the specified credentials from the credential vault.
        /// </summary>
        public static void DeleteCredential( string credentialName )
        {
            NativeMethods.DeleteCredential( credentialName );
        }


        private static class NativeMethods
        {
            // N.B.: CharSet.Unicode is important here - using default encoding always returns ERROR_GEN_FAILURE (31) when calling CredUIPromptForWindowsCredentials

            private const int UserNameMaxLength = 100;
            private const int DomainNameMaxLength = 100;
            private const int PasswordMaxLength = 100;

            private const int NoError = 0;
            private const int GenericCredential = 1;

            [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
            private struct CredUIInfo
            {
                public int Size;
                public IntPtr ParentHandle;
                public string MessageText;
                public string CaptionText;
                public IntPtr Banner; // Not used in this context
            }

            private enum CredentialsUI
            {
                Generic = 1,
                ShowSaveBox = 2
            }

            private enum CredentialPersistence
            {
                Session = 1,
                LocalMachine = 2,
                Enterprise = 3
            }

            [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
            private struct CredentialAttribute
            {
                public string Keyword;
                public uint Flags;
                public uint ValueSize;
                public IntPtr Value;
            }

            [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
            private struct NativeCredential
            {
                public uint Flags;
                public int Type;
                public string TargetName;
                public string Comment;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten; // .NET 2.0
                public uint CredentialBlobSize;
                public IntPtr CredentialBlob;
                public CredentialPersistence Persistence;
                public uint AttributeCount;
                public IntPtr CredAttribute;
                public string TargetAlias;
                public string UserName;
            }

            [DllImport( "ole32" )]
            private static extern void CoTaskMemFree( IntPtr ptr );

            [DllImport( "kernel32" )]
            private static extern void RtlZeroMemory( IntPtr dest, uint size );

            [DllImport( "credui", CharSet = CharSet.Unicode )]
            private static extern bool CredUnPackAuthenticationBuffer( int flags,
                                                                       IntPtr authBuffer,
                                                                       uint authBufferSize,
                                                                       IntPtr userName,
                                                                       ref int userNameMaxLength,
                                                                       IntPtr domainName,
                                                                       ref int domaiNameMaxLength,
                                                                       IntPtr password,
                                                                       ref int passwordMaxLength );

            [DllImport( "credui", CharSet = CharSet.Unicode )]
            private static extern uint CredUIPromptForWindowsCredentials( ref CredUIInfo info,
                                                                          int error,
                                                                          ref uint authPackage,
                                                                          IntPtr inAuthBuffer,
                                                                          uint inAuthBufferSize,
                                                                          out IntPtr outAuthBuffer,
                                                                          out uint outAuthBufferSize,
                                                                          ref bool save,
                                                                          CredentialsUI flags );

            [DllImport( "advapi32", CharSet = CharSet.Unicode )]
            private static extern bool CredRead( string target, int type, int reserved, out IntPtr credentialPtr );

            [DllImport( "advapi32", CharSet = CharSet.Unicode )]
            private static extern void CredFree( IntPtr buffer );

            [DllImport( "advapi32", CharSet = CharSet.Unicode )]
            private static extern bool CredWrite( ref NativeCredential userCredential, uint flags );

            [DllImport( "advapi32", CharSet = CharSet.Unicode )]
            private static extern bool CredDelete( string targetName, int type, int reserved );

            public static NetworkCredential ShowCredentialsPrompt( string caption, string message, ref bool save )
            {
                CredUIInfo credUI = new CredUIInfo
                {
                    Size = Marshal.SizeOf( typeof( CredUIInfo ) ),
                    CaptionText = caption,
                    MessageText = message
                };

                uint authPackage = 0;
                IntPtr outCredBuffer;
                uint outCredSize;
                var flags = CredentialsUI.Generic;
                if ( save )
                {
                    flags |= CredentialsUI.ShowSaveBox;
                }

                uint result = CredUIPromptForWindowsCredentials( ref credUI, NoError, ref authPackage, IntPtr.Zero, 0,
                                                                 out outCredBuffer, out outCredSize, ref save, flags );

                var usernameBuffer = Marshal.AllocCoTaskMem( UserNameMaxLength );
                var passwordBuffer = Marshal.AllocCoTaskMem( PasswordMaxLength );
                var domainBuffer = Marshal.AllocCoTaskMem( DomainNameMaxLength );

                int userNameLength = UserNameMaxLength;
                int domainLength = PasswordMaxLength;
                int passwordLength = DomainNameMaxLength;

                if ( result == 0 )
                {
                    if ( CredUnPackAuthenticationBuffer( 0, outCredBuffer, outCredSize,
                                                         usernameBuffer, ref userNameLength,
                                                         domainBuffer, ref domainLength,
                                                         passwordBuffer, ref passwordLength ) )
                    {
                        // Clear & free the memory
                        RtlZeroMemory( outCredBuffer, outCredSize );
                        CoTaskMemFree( outCredBuffer );

                        // -1 to remove the last \0 if the string is not empty
                        string userName = Marshal.PtrToStringUni( usernameBuffer, Math.Max( 0, userNameLength - 1 ) );
                        string password = Marshal.PtrToStringUni( passwordBuffer, Math.Max( 0, passwordLength - 1 ) );
                        string domain = Marshal.PtrToStringUni( domainBuffer, Math.Max( 0, domainLength - 1 ) );

                        return new NetworkCredential( userName, password, domain );
                    }
                }

                return null;
            }

            public static NetworkCredential ReadCredential( string credentialName )
            {
                IntPtr credentialPtr;
                if ( CredRead( credentialName, GenericCredential, 0, out credentialPtr ) )
                {
                    var credential = (NativeCredential) Marshal.PtrToStructure( credentialPtr, typeof( NativeCredential ) );

                    var nativeCred = new NetworkCredential
                    {
                        UserName = credential.UserName,
                        Password = Marshal.PtrToStringUni( credential.CredentialBlob, (int) credential.CredentialBlobSize / 2 ),
                    };

                    CredFree( credentialPtr );

                    return nativeCred;
                }
                return null;
            }

            public static void WriteCredential( string credentialName, NetworkCredential credential )
            {
                var nativeCred = new NativeCredential
                {
                    TargetName = credentialName,
                    UserName = credential.UserName,
                    CredentialBlob = Marshal.StringToCoTaskMemUni( credential.Password ),
                    CredentialBlobSize = (uint) Encoding.Unicode.GetByteCount( credential.Password ),
                    Type = GenericCredential,
                    Persistence = CredentialPersistence.LocalMachine
                };

                CredWrite( ref nativeCred, 0 );
            }

            public static void DeleteCredential( string credentialName )
            {
                CredDelete( credentialName, GenericCredential, 0 );
            }
        }
    }
}