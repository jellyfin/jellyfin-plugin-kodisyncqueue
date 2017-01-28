using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace NanoApi
{
    public class Impersonate
    {
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private IntPtr userToken = IntPtr.Zero;
        private string login;
        private string password;
        private string domain;

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        public Impersonate(string login, string password)
        {
            this.login = login;
            this.password = password;
            if (login.Contains('\\'))
            {
                string[] array = this.login.Split(new char[]
                {
                    '\\'
                });
                this.domain = array[0];
                this.login = array[1];
            }
        }

        public WindowsImpersonationContext GetImpersonate()
        {
            if (this.userToken == IntPtr.Zero && !Impersonate.LogonUser(this.login, this.domain, this.password, 2, 0, out this.userToken))
                return null;

            if (this.userToken == IntPtr.Zero)
                return null;

            return WindowsIdentity.Impersonate(this.userToken);
        }

        public static string GetFile(string path)
        {
            IntPtr zero = IntPtr.Zero;
            if (Impersonate.LogonUser("slan", "cib", "xxx", 2, 0, out zero))
            {
                using (WindowsIdentity.Impersonate(zero))
                {
                    try
                    {
                        string result = System.IO.File.ReadAllText(path);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        string result = "NOK : " + ex.Message;
                        return result;
                    }
                }
            }
            throw new SecurityException("Logon User Failed!");
        }


    }
}
