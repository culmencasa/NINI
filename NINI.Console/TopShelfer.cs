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
            var rc = HostFactory.Run(host =>                                    // 1
            {
                host.Service<Stub>(service =>                   // 2
                {
                    service.ConstructUsing(() => new Stub());   // 3
                    service.WhenStarted(s => s.Start());                        // 4
                    service.WhenStopped(s => s.Stop());                         // 5
                });

                host.RunAsLocalSystem();                                        // 6

                host.EnableServiceRecovery(service =>                           // 7
                {
                    service.RestartService(3);                                  // 8
                });
                host.SetDescription("Windows service based on topshelf");       // 9
                host.SetDisplayName("Topshelf demo service");                   // 10
                host.SetServiceName("TopshelfDemoService");                     // 11
                host.StartAutomaticallyDelayed();                               // 12
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());       // 13
            Environment.ExitCode = exitCode;
        }
    }
}
