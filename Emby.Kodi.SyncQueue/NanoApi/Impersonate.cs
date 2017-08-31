using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

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

        public IDisposable GetImpersonate()
        {
            return null;
        }
    }
}
