// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Playwright;

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
         private static async Task ScrapeCypressAsync()
        {
            // If needed, install browsers the first time:
            // await Playwright.InstallAsync();

            // 1. Create a Playwright instance
            using var playwright = await Playwright.CreateAsync();

            // 2. Launch a headless Chromium browser
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true // set false if you want to see the browser for debugging
            });

            // 3. Open a new tab
            var page = await browser.NewPageAsync();

            // 4. Navigate to Cypress Mountain's page
            await page.GotoAsync("https://www.cypressmountain.com/mountain-report");

            // 5. Wait for some selector that indicates lift data is loaded
            //    NOTE: Adjust based on the real DOM. If there's a known container or element,
            //    you might do something like:
            await page.WaitForSelectorAsync("section[data-name='liftStatus']");

            // 6A. Option 1: Directly query the final DOM using Playwright
            var liftContainers = await page.QuerySelectorAllAsync("div[data-lift-row]"); 
            // ^ Example selector. Adjust it to match the real DOM for lifts.

            foreach (var container in liftContainers)
            {
                // e.g. name in a <span> or some child element
                var liftNameEl = await container.QuerySelectorAsync(".lift-name");
                var liftName = liftNameEl != null ? await liftNameEl.InnerTextAsync() : "Unknown";

                // e.g. status from <img alt="Open" / "Closed">
                var statusImg = await container.QuerySelectorAsync("img.lift-status");
                var liftStatus = statusImg != null 
                    ? await statusImg.GetAttributeAsync("alt") 
                    : "Unknown";

                Console.WriteLine($"Lift: {liftName}, Status: {liftStatus}");
            }

            // 6B. Option 2: Get the fully rendered HTML and parse with HtmlAgilityPack
            // string renderedHtml = await page.ContentAsync();
            // var doc = new HtmlAgilityPack.HtmlDocument();
            // doc.LoadHtml(renderedHtml);
            // ... parse with doc.DocumentNode.SelectNodes(...) ...
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