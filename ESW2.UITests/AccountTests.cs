using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System;

namespace ESW2.UITests
{
    [TestClass]
    public class AccountTests : BaseTest // Inherit from BaseTest
    {
        [TestMethod]
        public void Login_WithValidCredentials_ShouldSucceed()
        {
            // Navigate to the login page (BaseTest already navigates to BaseUrl)
            Driver.Navigate().GoToUrl(Driver.Url + "Account/Login");

            // Find elements
            IWebElement usernameField = Driver.FindElement(By.Name("username"));
            IWebElement passwordField = Driver.FindElement(By.Id("password"));
            IWebElement submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));

            // Enter credentials
            // TODO: Ensure this user exists and has this password, or create/register one programmatically in a setup step if needed.
            usernameField.SendKeys("admin@admin.com");
            passwordField.SendKeys("Admin123$");

            // Click submit
            submitButton.Click();

            // Assertion: Check if the URL is the home page
            // The BaseUrl is "http://localhost:5000"
            // After successful login, it should redirect to "/" relative to the base URL.
            string expectedUrl = Driver.Url.TrimEnd('/') + "/";
            // It seems Driver.Url might already be the base URL from BaseTest's Initialize,
            // or it could be the current page's URL.
            // Let's refine the expected URL based on BaseTest's BaseUrl.
            // BaseTest navigates to "http://localhost:5000" initially.
            // Login page is "http://localhost:5000/Account/Login".
            // After login, it should go to "http://localhost:5000/".

            // A small wait to allow redirection to complete, if necessary.
            // WebDriverWait can be used for more robust waits for elements or conditions.
            System.Threading.Thread.Sleep(1000); // Simple wait, consider WebDriverWait for production tests

            // Assert the current URL
            // BaseURL in BaseTest is "http://localhost:5000"
            // We expect to be redirected to "http://localhost:5000/"
            // If Driver.Url is "http://localhost:5000/Account/Login" at the start of the test method (after GoToUrl),
            // then after login, it should become "http://localhost:5000/".

            // Let's get the base part of the current URL to construct the expected home page URL
            Uri currentUri = new Uri(Driver.Url);
            string expectedHomePageUrl = new Uri(currentUri, "/").ToString();

            // If BaseTest.BaseUrl is consistently "http://localhost:5000", we can use that directly.
            // string applicationBaseUrl = "http://localhost:5000"; // As defined in BaseTest
            // string expectedHomePageUrl = applicationBaseUrl.TrimEnd('/') + "/";


            Assert.AreEqual(expectedHomePageUrl, Driver.Url, "User was not redirected to the home page after login.");
        }

        [TestMethod]
        public void Register_WithValidDetails_ShouldSucceed()
        {
            // Navigate to the registration page
            Driver.Navigate().GoToUrl(Driver.Url.TrimEnd('/') + "/Account/Register");

            // Generate unique user data
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string username = $"testuser_{timestamp}";
            string email = $"testuser_{timestamp}@example.com";
            string password = "ValidPassword123!";
            string nif = new Random().Next(100000000, 999999999).ToString(); // Generate a random 9-digit NIF
            string morada = "Rua Teste, 123";

            // Find elements
            IWebElement usernameField = Driver.FindElement(By.Name("username"));
            IWebElement emailField = Driver.FindElement(By.Name("email"));
            IWebElement nifField = Driver.FindElement(By.Name("nif"));
            IWebElement moradaField = Driver.FindElement(By.Name("morada"));
            IWebElement passwordField = Driver.FindElement(By.Id("password")); // or By.Name("password")
            IWebElement submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));

            // Enter details
            usernameField.SendKeys(username);
            emailField.SendKeys(email);
            nifField.SendKeys(nif);
            moradaField.SendKeys(morada);
            passwordField.SendKeys(password);

            // Click submit
            submitButton.Click();

            // A small wait to allow redirection to complete, if necessary.
            System.Threading.Thread.Sleep(1000); // Consider WebDriverWait for production tests

            // Assert that the URL is the login page
            // BaseURL in BaseTest is "http://localhost:5000"
            // After successful registration, it should redirect to "/Account/Login".
            Uri currentUri = new Uri(Driver.Url);
            string expectedLoginPageUrl = new Uri(currentUri, "/Account/Login").ToString();

            // If BaseTest.BaseUrl is consistently "http://localhost:5000", we can use that directly.
            // string applicationBaseUrl = "http://localhost:5000"; // As defined in BaseTest
            // string expectedLoginPageUrl = applicationBaseUrl.TrimEnd('/') + "/Account/Login";

            Assert.AreEqual(expectedLoginPageUrl, Driver.Url, "User was not redirected to the login page after registration.");
        }
    }
}
