// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace MountainLiftScraper
{
    // A simple model to hold each lift's data
    class Program
    {
        static async Task Main(string[] args)
        {
            string url = "https://www.cypressmountain.com/mountain-report";

            // 1. Get the fully rendered HTML (via Playwright)
            var renderedHtml = await GetRenderedHtmlAsync(url);
            if (string.IsNullOrWhiteSpace(renderedHtml))
            {
                Console.WriteLine("Failed to retrieve rendered HTML.");
                return;
            }

            // 2. Parse that HTML with HtmlAgilityPack
            var liftStatuses = ParseLiftData(renderedHtml);

            // 3. Print results
            Console.WriteLine($"Scraped from: {url}");
            foreach (var lift in liftStatuses)
            {
                Console.WriteLine($"Lift: {lift.Name}, Status: {lift.Status}");
            }
        }

        /// <summary>
        /// Launches a headless browser, navigates to the page,
        /// waits for the lift status section, then grabs the final HTML.
        /// </summary>
        private static async Task<string> GetRenderedHtmlAsync(string url)
        {
            // If it's your first time using Playwright in this project:
            // await Playwright.InstallAsync();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true // set to false if you want to see the browser
                });
                var page = await browser.NewPageAsync();

                // Navigate to Cypress Mountain's page
                await page.GotoAsync(url);

                // Wait for the "liftStatus" section to appear
                await page.WaitForSelectorAsync("section[data-name='liftStatus']");

                // At this point, the JS should have loaded the actual lift data
                var renderedHtml = await page.ContentAsync();
                return renderedHtml;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching rendered HTML: " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses the fully rendered HTML (from Playwright) using HtmlAgilityPack,
        /// extracting each lift's name and status from the DOM.
        /// </summary>
        private static List<LiftStatus> ParseLiftData(string html)
        {
            var results = new List<LiftStatus>();

            // 1. Load HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 2. Find the "liftStatus" section
            var liftSection = doc.DocumentNode.SelectSingleNode("//section[@data-name='liftStatus']");
            if (liftSection == null)
            {
                Console.WriteLine("Could not find the liftStatus section in the HTML.");
                return results;
            }

            // 3. Identify each <li> that represents a single lift row
            //    From your screenshot: <li class="flex items-center gap-1 border-t ... py-2">
            //    We'll do partial matches on classes like 'flex' and 'items-center'.
            var liNodes = liftSection.SelectNodes(".//li[contains(@class, 'flex') and contains(@class, 'items-center')]");
            if (liNodes == null)
            {
                Console.WriteLine("No matching <li> elements found in liftStatus section.");
                return results;
            }

            // 4. For each <li>, extract name and status
            foreach (var li in liNodes)
            {
                // A) Lift name typically in a <p> or <span><p> structure
                var nameNode = li.SelectSingleNode(".//p");
                string liftName = nameNode?.InnerText?.Trim() ?? "Unknown Lift";

                // B) Status from an <img alt="Open" or alt="Closed">
                var imgNode = li.SelectSingleNode(".//img[@alt]");
                string liftStatus = imgNode?.GetAttributeValue("alt", "Unknown") ?? "Unknown";

                results.Add(new LiftStatus
                {
                    Name = liftName,
                    Status = liftStatus
                });
            }
        //there's a better way to do this
            return results;
        }
    }
}