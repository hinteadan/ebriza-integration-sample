using EbrizaAPI;
using System;
using System.Linq;
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
         * 
         * appClientId - identifies the Ebriza Client Company Account on which the app is installed (e.g.: Meron, Narcofee, Metro, Selgros, Auchan, etc.); is unique for an Ebriza Client;
         * 
         * */
        static readonly Guid appPublicKey = new Guid("53C1C668-F3B3-486F-9652-6A327819DA0D");
        const string appSecretKey = "IUP0JS3I6HYV6850GE3ZJ7VBH7V3WZJ4G2C7RWVQN6ZG81Z640H4Z94L8NTL69VG74PE57JTSNLMHODQ";
        static readonly Guid appClientId = new Guid("DF3EB2B2-9153-4C80-9674-99DAD7824177");


        /*
         * 
         * We need a separate app if we want to "write"/open new orders on the Ebriza POS
         * This app MUST be of transaction or percent transaction pricing type, and it will be installed PER each location on the client company account, so the client ID will uniquely identify the Client+Location Pair.
         * 
         */
        static readonly Guid billOpenerAppPublicKey = new Guid("A5B6C11C-D1D9-40DE-92BC-89AC0EC454CE");
        const string billOpenerAppSecretKey = "BAATNO098TICB3IJ3HM7X6YC1TNZYGBELFLRWZ7FCO8UUROYY895MSBEL824BBL1RD06R0WEJH5I8ERK";
        static readonly Guid billOpenerAppClientId = new Guid("4849F2E2-5508-449F-837F-FB8C7DD0DD09");
        #endregion

        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            //Construct the Ebriza Client API interactor. Available as NuGet Package: https://www.nuget.org/packages/EbrizaAPI
            EbrizaAPI.Client ebrizaDataReaderClient = new EbrizaAPI.Client(appPublicKey, appSecretKey, appClientId);
            EbrizaAPI.Client ebrizaBillOpenerClient = new EbrizaAPI.Client(billOpenerAppPublicKey, billOpenerAppSecretKey, billOpenerAppClientId);

            //Now we have access to the Ebriza Client's; Available Endpoints: https://ebriza.com/docs/api

            //First, we must get all the client's locations, because we'll need them for most of the API requests (API: https://ebriza.com/docs/api#Getcompanylocations):
            //A company can (and many do) have multiple locations, with specific products, prices, etc.
            CompanyLocation[] companyLocations = await ebrizaDataReaderClient.Get<CompanyLocation[]>("company/locations");

            Console.WriteLine();
            Console.WriteLine($"{companyLocations.Length} locations: {string.Join(", ", companyLocations.Select(x => x.Name))}");
            Console.WriteLine();

            //Let's get the company's products and categories for the first location (https://ebriza.com/docs/api#Listitems):
            Item[] productsAndCategories = await ebrizaDataReaderClient.Get<Item[]>("items",
                new System.Collections.Generic.Dictionary<string, object> {
                    { "locationid", companyLocations.First().ID }
                }
            );

            Item[] products = productsAndCategories.Where(x => !x.IsCategory).ToArray();
            Item[] categories = productsAndCategories.Where(x => x.IsCategory).ToArray();


            Console.WriteLine();
            Console.WriteLine($"{categories.Length} categories: {string.Join(", ", categories.Select(x => x.Name))}");
            Console.WriteLine();
            Console.WriteLine($"{products.Length} products: {string.Join(", ", products.Select(x => x.Name))}");
            Console.WriteLine();



            //Now, the juciy stuff: let's open a bill on the company's POS
            //First we need the list of tables on the location to know on which table we put the order:
            //If we want to open a delivery bill (that's a bill that will be delivered to the client, and not bound to a table at the location), than we MUST omit this part (not send the TableID)
            LocationTable[] tables = await ebrizaDataReaderClient.Get<LocationTable[]>("tables", new System.Collections.Generic.Dictionary<string, object> {
                { "locationid", companyLocations.First().ID }
            });
            LocationTable tableToPutTheBillOn = tables.First();
            string result = await ebrizaBillOpenerClient.Post("bills/open", new OpenBillRequest
            {
                TableID = tableToPutTheBillOn.ID,
                Items = new OpenBillItem[] {
                    new OpenBillItem {
                        ID = products.First().ID,
                        Quantity = 10,
                    }
                },
            });


            //Console.WriteLine();
            Console.WriteLine($"Open Bill Result: {result}");
            Console.WriteLine();





            //Now let's play with WebHooks
            //First we need to start an HTTP Server. We'll run a Nancy Self Hosted HTTP server, publicly exposed using ngrok;
            //In a real scenario this is usually replaced by the Web App itself, served via IIS or Apache or some other production wise HTTP Server.
            Console.WriteLine();
            Console.WriteLine($"Webhooks Playground Starts here");
            HttpServer httpServer = new HttpServer();

            //This is, of course, not needed in a real app, since the URL is already exposed to the Internet
            httpServer.OnPublicUrlAcquired += async (_, arg) =>
            {
                string webhookUrl = $"{arg.PublicUrl}/ebrizawebhook";

                //Let's register our webhook with Ebriza
                await ebrizaDataReaderClient.Post("webhooks/subscribe", new WebHookSubscribeRequest { CallbackUrl = webhookUrl, Entity = WebHookEntityType.Bill });
                await ebrizaDataReaderClient.Post("webhooks/subscribe", new WebHookSubscribeRequest { CallbackUrl = webhookUrl, Entity = WebHookEntityType.Order });

                //Now let's open a Bill so we trigger the webhook
                await OpenNewBill(ebrizaDataReaderClient, ebrizaBillOpenerClient, companyLocations, products);
            };

            HttpServer.OnEbrizaWebhook += (_, arg) =>
            {
                Console.WriteLine("Received Notification from Ebriza");
                Console.WriteLine($"Changed Entity: {arg.Entity} with id {arg.ID}");
                //Here we can use the API to fetcg the changed Order or Bill Details and see its changes
            };

            httpServer.Start();
            Console.ReadLine();
            httpServer.Stop();

            //Let's unregister our webhook with Ebriza
            await ebrizaDataReaderClient.Post("webhooks/unsubscribe", new WebHookUnsubscribeRequest { Entity = WebHookEntityType.Bill });
            await ebrizaDataReaderClient.Post("webhooks/unsubscribe", new WebHookUnsubscribeRequest { Entity = WebHookEntityType.Order });





            Console.WriteLine();
            Console.WriteLine($"Done @ {DateTime.Now}");
            Console.ReadLine();
        }

        private static async Task OpenNewBill(Client ebrizaDataReaderClient, Client ebrizaBillOpenerClient, CompanyLocation[] companyLocations, Item[] products)
        {
            LocationTable[] tables = await ebrizaDataReaderClient.Get<LocationTable[]>("tables", new System.Collections.Generic.Dictionary<string, object> {
                { "locationid", companyLocations.First().ID }
            });
            LocationTable tableToPutTheBillOn = tables.First();
            string result = await ebrizaBillOpenerClient.Post("bills/open", new OpenBillRequest
            {
                TableID = tableToPutTheBillOn.ID,
                Items = new OpenBillItem[] {
                    new OpenBillItem {
                        ID = products.First().ID,
                        Quantity = 10,
                    }
                },
            });
        }
    }

    class CompanyLocation
    {
        public Guid ID { get; set; }
        public string Name { get; set; }

        //We can map the Address here as well if needed.
    }

    class Item
    {
        public Guid ID { get; set; }
        public Guid? CategoryID { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string SKU { get; set; }
        public decimal? Price { get; set; }
        public bool IsCategory { get; set; }
        public bool HasProducts { get; set; }

        //We can map additional properties here if needed; they're all described in the online documentation.
    }

    class LocationTable
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid RoomID { get; set; }
        public string RoomName { get; set; }
        public bool Occupied { get; set; }
    }

    class OpenBillRequest
    {
        public Guid TableID { get; set; }
        public OpenBillItem[] Items { get; set; } = new OpenBillItem[0];
    }

    class OpenBillItem
    {
        public Guid? ID { get; set; }
        public string SKU { get; set; }
        public decimal Quantity { get; set; } = 1;
    }

    public enum WebHookEntityType
    {
        Order = 0, //Orders are closed sales, paid for
        Bill = 1, //Bills are opened sales which are not yet closes, not yet paid for
    }

    class WebHookSubscribeRequest
    {
        public string CallbackUrl { get; set; }
        public WebHookEntityType Entity { get; set; }
    }

    class WebHookUnsubscribeRequest
    {
        public WebHookEntityType Entity { get; set; }
    }

    public class WebHookEventArgs : EventArgs
    {
        public Guid ID { get; set; }
        public Guid EbrizaclientID { get; set; }
        public WebHookEntityType Entity { get; set; }
    }
}
