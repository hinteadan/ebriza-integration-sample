﻿using System;
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
         * clientId - identifies the Ebriza Client Company Account on which the app is installed (e.g.: Meron, Narcofee, Metro, Selgros, Auchan, etc.); is unique for an Ebriza Client;
         * 
         * */
        static readonly Guid appPublicKey = new Guid("53C1C668-F3B3-486F-9652-6A327819DA0D");
        const string appSecretKey = "IUP0JS3I6HYV6850GE3ZJ7VBH7V3WZJ4G2C7RWVQN6ZG81Z640H4Z94L8NTL69VG74PE57JTSNLMHODQ";
        static readonly Guid clientId = new Guid("DF3EB2B2-9153-4C80-9674-99DAD7824177");
        #endregion

        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            //Construct the Ebriza Client API interactor. Available as NuGet Package: https://www.nuget.org/packages/EbrizaAPI
            EbrizaAPI.Client ebrizaClient = new EbrizaAPI.Client(appPublicKey, appSecretKey, clientId);

            //Now we have access to the Ebriza Client's; Available Endpoints: https://ebriza.com/docs/api

            //First, we must get all the client's locations, because we'll need them for most of the API requests (API: https://ebriza.com/docs/api#Getcompanylocations):
            CompanyLocation[] companyLocations = await ebrizaClient.Get<CompanyLocation[]>("company/locations");

            //Let's get the company's products and categories (https://ebriza.com/docs/api#Listitems):
            Item[] productsAndCategories = await ebrizaClient.Get<Item[]>("items", new System.Collections.Generic.Dictionary<string, object> { { "locationid", companyLocations.First().ID } });

            Item[] products = productsAndCategories.Where(x => !x.IsCategory).ToArray();
            Item[] categories = productsAndCategories.Where(x => x.IsCategory).ToArray();

            Console.WriteLine($"Done @ {DateTime.Now}");
            Console.ReadLine();
        }
    }

    class CompanyLocation
    {
        public Guid ID { get; set; }
        public string Name { get; set; }

        //We can map the Address here as well if needed
    }

    class Item
    {
        public Guid? ID { get; set; }
        public Guid? CategoryID { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string SKU { get; set; }
        public decimal? Price { get; set; }
        public bool IsCategory { get; set; }
        public bool HasProducts { get; set; }
    }
}
