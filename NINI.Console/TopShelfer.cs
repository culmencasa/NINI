using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace NINI.Console
{
    class TopShelfer
    {
        internal static void Configure()
        {
            var rc = HostFactory.Run(host =>                                    
            {
                host.Service<Stub>(service =>                   
                {
                    service.ConstructUsing(() => new Stub());   
                    service.WhenStarted(s => s.Start());                        
                    service.WhenStopped(s => s.Stop());                         
                });

                host.RunAsLocalSystem();                                        

                host.EnableServiceRecovery(service =>                           
                {
                    service.RestartService(3);                                  
                });
                host.SetDescription("守护进程, 提升主程序权限.");       
                host.SetDisplayName("NINI Console Service");                   
                host.SetServiceName("NINIConsoleService");                     
                host.StartAutomaticallyDelayed();                               
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());       // 13
            Environment.ExitCode = exitCode;
        }
    }
}
