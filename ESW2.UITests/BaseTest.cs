using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace ESW2.UITests
{
    [TestClass]
    public class BaseTest
    {
        public IWebDriver Driver { get; private set; }
        private const string BaseUrl = "http://localhost:5000"; // Assuming default Kestrel port

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up ChromeDriver
            // The ChromeDriver path is usually handled by the Selenium.WebDriver.ChromeDriver NuGet package
            // if the chromedriver.exe is in the build output directory or in a directory in the PATH.
            // For some environments, especially in CI/CD, you might need to specify the driver location explicitly.
            var chromeOptions = new ChromeOptions();
            // Add any desired options, e.g., headless mode for CI
            // chromeOptions.AddArgument("--headless");
            // chromeOptions.AddArgument("--disable-gpu"); // often necessary for headless
            // chromeOptions.AddArgument("--window-size=1920,1080"); // example window size

            Driver = new ChromeDriver(chromeOptions);

            // Set implicit wait
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            // Navigate to the base URL
            Driver.Navigate().GoToUrl(BaseUrl);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Quit the driver to close the browser and dispose of the WebDriver instance
            Driver?.Quit();
        }
    }
}
