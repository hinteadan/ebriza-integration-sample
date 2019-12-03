using System;
using System.Threading.Tasks;

namespace EbrizaIntegrationSampleApp
{
    class Program
    {
        #region App Integration Keys
        /*
         * 
         * All these keys are available from the Company Account on which the app is installed
         * 
         * appPublicKey - identifies the app itself; is unique for each Ebriza App
         * appSecretKey - identifies the app itself (serves as the app's password); is unique for each Ebriza App
         * clientId - identifies the Ebriza Client Company Account on which the app is installed (e.g.: Meron, Narcofee, etc.); is unique for an Ebriza Client;
         * 
         * */
        const string appPublicKey = "53c1c668-f3b3-486f-9652-6a327819da0d";
        const string appSecretKey = "53c1c668-f3b3-486f-9652-6a327819da0d";
        const string clientId = "53c1c668-f3b3-486f-9652-6a327819da0d";
        #endregion

        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            await Task.Delay(1000);

            Console.WriteLine($"Done @ {DateTime.Now}");
            Console.ReadLine();
        }
    }
}
