using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Reflection;
using System.IO;

namespace VVVV.Nodes.OpenCV
{
    [Startable(Lazy=false)]
    public class ManageDependencies : IStartable
    {
        public void Start()
        {
            bool isx64 = IntPtr.Size == 8;
            var pathToThisAssembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pathToBinFolder = Path.Combine(pathToThisAssembly, "Dependencies", "OpenCV", isx64 ? "x64" : "x86");
            var envPath = Environment.GetEnvironmentVariable("PATH");
            envPath = string.Format("{0};{1}", envPath, pathToBinFolder);
            Environment.SetEnvironmentVariable("PATH", envPath);
        }

        public void Shutdown()
        {
        }
    }
}
