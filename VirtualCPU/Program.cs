using System;
using Mono.Unix;
using Mono.Unix.Native;
using Nancy.Hosting.Self;

namespace VirtualCPU
{
    static class Program
    {
        static void Main(string[] args)
        {
            var address = new Uri(Environment.GetEnvironmentVariable("HOST_ADDRESS"));
            var host = new NancyHost(address);

            host.Start();

            if (Type.GetType("Mono.Runtime") != null)
            {
                UnixSignal.WaitAny(new[]
                {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM),
                    new UnixSignal(Signum.SIGQUIT),
                    new UnixSignal(Signum.SIGHUP)
                });
            }
            else
                Console.ReadLine();

            host.Stop();
        }
    }
}
