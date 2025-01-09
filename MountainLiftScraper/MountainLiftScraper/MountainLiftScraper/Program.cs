// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
namespace MountainLiftScraper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string url = "https://www.cypressmountain.com/mountain-report"; 
            
            var liftStatuses = await GetLiftStatuses(url);

            Console.WriteLine($"Scraped from: {url}");
            foreach (var lift in liftStatuses)
            {
                Console.WriteLine($"Lift: {lift.Name}, Status: {lift.Status}");
            }
        }

        private static async Task<List<LiftStatus>> GetLiftStatuses(string url)
        {
            var httpClient = new HttpClient();
            var htmlContent = await httpClient.GetStringAsync(url);

            // Load the HTML into an HtmlDocument
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var result = new List<LiftStatus>();

            // ---- EXAMPLE SELECTOR: Adjust this to match the actual HTML structure ----
            // Let's assume each lift is in a <div class="lift-box">
            var liftNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"lift-status\"]");
           
            if (liftNodes == null)
            {
                Console.WriteLine("No lift rows found. Page may be dynamic or structure may differ.");
                return result;
            }

            foreach (var node in liftNodes)
            { 
                // A) Lift name often appears in a <span> (like <span>Eagle Express</span>)
                var nameNode = node.SelectSingleNode(".//div[contains(@class, 'col-span-1 flex flex-col gap-10 lg:col-span-7')]");
                var liftName = nameNode?.InnerText?.Trim() ?? "Unknown Lift";

                // B) The status is in the <img alt="Open/Closed">
                //    e.g. <img alt="Closed">
                var imgNode = node.SelectSingleNode(".//img[@alt='Open' or @alt='Closed']");
                // If the <img> is found, get its "alt" attribute
                var liftStatus = imgNode?.GetAttributeValue("alt", "Unknown") ?? "Unknown";

                // Add to our results
                result.Add(new LiftStatus
                {
                    Name = liftName,
                    Status = liftStatus
                });
            }
            

            return result;
        }
    }
}