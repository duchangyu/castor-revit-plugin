using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Utils
{
    public static class AccessUtils
    {
        public static bool CheckWriteAccess(string path)
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var accessControl = new DirectoryInfo(path).GetAccessControl();
            var accessRules = accessControl.GetAccessRules(true, true, typeof(NTAccount));
            var writeAccess = false;
            foreach (FileSystemAccessRule rule in accessRules)
                if (principal.IsInRole(rule.IdentityReference.Value) && (rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData)
                {
                    writeAccess = true;
                    break;
                }

            return writeAccess;
        }
    }
}
