using System;
using System.Threading.Tasks;

namespace EbrizaIntegrationSampleApp
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            await Task.Delay(1000);

            Console.WriteLine($"Done @ {DateTime.Now}");
            Console.ReadLine();
        }
    }
}
