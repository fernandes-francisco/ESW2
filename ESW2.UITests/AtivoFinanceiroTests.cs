using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; // Required for SelectElement
using System;
using System.Linq; // Required for LINQ operations if any

namespace ESW2.UITests
{
    [TestClass]
    public class AtivoFinanceiroTests : BaseTest // Inherit from BaseTest
    {
        [TestMethod]
        public void Create_NewFinancialAsset_DepositoAPrazo_ShouldSucceed()
        {
            // --- 1. Perform Login ---
            Driver.Navigate().GoToUrl(Driver.Url.TrimEnd('/') + "/Account/Login");

            IWebElement usernameLoginField = Driver.FindElement(By.Name("username"));
            IWebElement passwordLoginField = Driver.FindElement(By.Id("password"));
            IWebElement submitLoginButton = Driver.FindElement(By.CssSelector("form[action='/Account/Login'] button[type='submit']"));
            // More specific selector for the login button if there are multiple forms

            usernameLoginField.SendKeys("admin@admin.com");
            passwordLoginField.SendKeys("Admin123$");
            submitLoginButton.Click();

            // Wait for login to complete - simple wait for URL change to home page
            // A more robust wait would be WebDriverWait for a specific element on the home page.
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.EndsWith("/")); // Assuming home page is base URL + "/"

            // --- 2. Navigate to Create Financial Asset Page ---
            // The page defaults to "Deposito" type, or we can specify:
            Driver.Navigate().GoToUrl(Driver.Url.TrimEnd('/') + "/AtivoFinanceiro/Create?tipoAtivo=Deposito");

            // --- 3. Fill Asset Details for "Deposito a Prazo" ---
            // Common Fields
            Driver.FindElement(By.Id("data_inicio")).SendKeys(DateTime.Today.ToString("yyyy-MM-dd"));
            Driver.FindElement(By.Id("duracao_meses")).SendKeys("12");
            Driver.FindElement(By.Name("percentual_imposto")).SendKeys("28"); // Assuming 'percentual_imposto' is the name due to asp-for

            // Deposito a Prazo Specific Fields
            // We need to ensure there's at least one bank. For now, assume bank with value "1" exists.
            // If no banks, this will fail. A robust test would pre-seed banks or check for their existence.
            var bankDropdown = Driver.FindElement(By.Id("id_banco_novo"));
            var selectBank = new SelectElement(bankDropdown);
            // Check if there are options before trying to select one
            if (selectBank.Options.Count > 1) // Greater than 1 because the first is "-- Selecione um Banco --"
            {
                 // Try to select by value "1", or the first actual bank if "1" is not available.
                var optionToSelect = selectBank.Options.FirstOrDefault(opt => opt.GetAttribute("value") == "1" && opt.GetAttribute("value") != "");
                if (optionToSelect != null)
                {
                    selectBank.SelectByValue("1");
                }
                else if (selectBank.Options.Count > 1 && selectBank.Options[1].GetAttribute("value") != "")
                {
                    // Select the first available actual bank
                    selectBank.SelectByIndex(1);
                }
                else
                {
                    Assert.Inconclusive("No banks available in the dropdown to select for Deposito a Prazo.");
                }
            }
            else
            {
                Assert.Inconclusive("No banks available in the dropdown to select for Deposito a Prazo.");
            }


            Driver.FindElement(By.Id("numero_conta_banco_novo")).SendKeys(new Random().Next(100000, 999999).ToString());
            Driver.FindElement(By.Id("titulares_novo")).SendKeys("Test Titular");
            Driver.FindElement(By.Id("valor_deposito_novo")).SendKeys("5000.50");
            Driver.FindElement(By.Id("taxa_juro_anual_novo")).SendKeys("1.5");

            // --- 4. Submit Form ---
            // The submit button is <button type="submit" class="btn btn-primary mt-3">Inserir</button>
            IWebElement submitAssetButton = Driver.FindElement(By.CssSelector("form[action='/AtivoFinanceiro/Create'] button[type='submit']"));
            submitAssetButton.Click();

            // --- 5. Assert Creation ---
            // Wait for navigation to the index page
            wait.Until(d => d.Url.Contains("/AtivoFinanceiro"));
            // More specific: check for URL being exactly the base URL + "/AtivoFinanceiro" or "/AtivoFinanceiro/Index"

            string expectedUrlEnd = "/AtivoFinanceiro"; // Or "/AtivoFinanceiro/Index"
            bool urlEndsAsExpected = Driver.Url.EndsWith(expectedUrlEnd) || Driver.Url.EndsWith(expectedUrlEnd + "/Index");

            // Also, check for a success message if one is usually displayed
            // Example: bool successMessageDisplayed = Driver.FindElements(By.CssSelector(".alert-success")).Any();
            // Assert.IsTrue(successMessageDisplayed, "Success message was not displayed.");

            Assert.IsTrue(urlEndsAsExpected, $"User was not redirected to the AtivoFinanceiro index page. Current URL: {Driver.Url}");
        }
    }
}
