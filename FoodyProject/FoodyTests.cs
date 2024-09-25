using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace FoodyProject
{
    public class FoodyTests
    {
        protected IWebDriver driver;
        private static readonly string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:85/";
        private static string? lastCreatedFoodTitle;
        private static string? lastCreatedFoodDescription;

        private Actions actions;

       [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddUserProfilePreference("profile.password_manager_enabled", false);

            driver = new ChromeDriver(chromeOptions);

            driver.Navigate().GoToUrl($"{BaseUrl}User/Login");
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            driver.FindElement(By.Id("username")).SendKeys("sanya.panova");
            driver.FindElement(By.Id("password")).SendKeys("123456");
            driver.FindElement(By.XPath("//button[@class='btn btn-primary btn-block fa-lg gradient-custom-2 mb-3']")).Click();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            driver.Quit();
            driver.Dispose();
        }

        [Test, Order(1)]
        public void AddFoodWithInvalidDataTest()
        {
            string invalidTitle = "";
            string invalidDescription = "";

            driver.Navigate().GoToUrl($"{BaseUrl}Food/Add");

            driver.FindElement(By.XPath("//input[@id='name']")).SendKeys(invalidTitle);
            driver.FindElement(By.XPath("//input[@id='description']")).SendKeys(invalidDescription);
            driver.FindElement(By.XPath("//button[@class='btn btn-primary btn-block fa-lg gradient-custom-2 mb-3']")).Click();

            string currentUrl = driver.Url;
            Assert.That(currentUrl, Is.EqualTo($"{BaseUrl}Food/Add"), "The page should remain on the creation page with invalid data.");

            var mainErrorMessage = driver.FindElement(By.CssSelector(".validation-summary-errors li"));
            Assert.That(mainErrorMessage.Text.Trim(), Is.EqualTo("Unable to add this food revue!"), "The main error message is not displayed as expected.");

            var titleErrorMessage = driver.FindElements(By.XPath("//span[@class='text-danger field-validation-error']"))[0];
            Assert.That(titleErrorMessage.Text.Trim(), Is.EqualTo("The Name field is required."), "The title error message is not displayed as expected.");

            var descriptionErrorMessage = driver.FindElements(By.XPath("//span[@class='text-danger field-validation-error']"))[1];
            Assert.That(descriptionErrorMessage.Text.Trim(), Is.EqualTo("The Description field is required."), "The description error message is not displayed as expected.");
        }

        [Test, Order(2)]
        public void AddRandomFoodTest()
        {
            lastCreatedFoodTitle = "Food " + GenerateRandomString(5);
            lastCreatedFoodDescription = "Description " + GenerateRandomString(10);

            driver.Navigate().GoToUrl($"{BaseUrl}Food/Add");

            driver.FindElement(By.XPath("//input[@id='name']")).SendKeys(lastCreatedFoodTitle);
            driver.FindElement(By.XPath("//input[@id='description']")).SendKeys(lastCreatedFoodDescription);
            driver.FindElement(By.XPath("//button[@class='btn btn-primary btn-block fa-lg gradient-custom-2 mb-3']")).Click();

            string currentUrl = driver.Url;
            Assert.That(currentUrl, Is.EqualTo(BaseUrl), "The page should remain on the creation page with invalid data."); 

            var addedLastFoodTitle = driver.FindElement(By.XPath($"//h2[contains(text(),'{lastCreatedFoodTitle}')]"));            
            Assert.IsNotNull(addedLastFoodTitle, "The newly added food is not found on the home page.");

            Assert.That(addedLastFoodTitle.Text.Trim(), Is.EqualTo(lastCreatedFoodTitle), "The displayed title does not match the input.");
        }

        [Test, Order(3)]
        public void EditLastAddedFoodTest()
        {  
            driver.Navigate().GoToUrl(BaseUrl);

            var foodItems = driver.FindElements(By.XPath("//div[@class='row gx-5 align-items-center']"));
            Assert.IsTrue(foodItems.Count > 0, "No foods were found on the page.");
            var lastFoodItem = foodItems.Last();

            var lastFoodTitleElement = lastFoodItem.FindElement(By.XPath(".//h2"));
            lastCreatedFoodTitle = lastFoodTitleElement.Text.Trim();

            var editButton = lastFoodItem.FindElement(By.XPath(".//a[contains(text(),'Edit')]"));
            actions = new Actions(driver);
            actions.MoveToElement(editButton).Perform();
            editButton.Click();

            string newTitle = lastCreatedFoodTitle + " Edited";
            var titleInput = driver.FindElement(By.XPath("//input[@id='name']"));
            titleInput.Clear();
            titleInput.SendKeys(newTitle);

            driver.FindElement(By.XPath("//button[@class='btn btn-primary btn-block fa-lg gradient-custom-2 mb-3']")).Click();

            driver.Navigate().GoToUrl(BaseUrl);

            foodItems = driver.FindElements(By.XPath("//div[@class='row gx-5 align-items-center']"));
            lastFoodItem = foodItems.Last();
            lastFoodTitleElement = lastFoodItem.FindElement(By.XPath(".//h2"));
            string updatedTitle = lastFoodTitleElement.Text.Trim();

            Assert.That(updatedTitle, Is.EqualTo(lastCreatedFoodTitle), "The title was incorrectly updated. The functionality to edit the title should not work as expected.");

            Console.WriteLine($"Test passed: The title remains unchanged as expected. Original Title: '{lastCreatedFoodTitle}', Attempted New Title: '{newTitle}', Displayed Title: '{updatedTitle}'");
        }

        [Test, Order(4)]
        public void SearchForFoodTitleTest()
        {
            driver.Navigate().GoToUrl(BaseUrl);

            var searchBar = driver.FindElement(By.XPath("//input[@class='form-control rounded mt-5 xl']"));
            searchBar.Clear();
            searchBar.SendKeys(lastCreatedFoodTitle);

            var searchButton = driver.FindElement(By.XPath("//button[@class='btn btn-primary rounded-pill mt-5 col-2']"));
            searchButton.Click();

            var searchResults = driver.FindElements(By.XPath("//div[@class='row gx-5 align-items-center']"));

            Assert.That(searchResults.Count, Is.EqualTo(1), "The search did not return exactly one result.");

            var resultTitle = searchResults.First().FindElement(By.XPath(".//h2")).Text.Trim();
            Assert.That(resultTitle, Is.EqualTo(lastCreatedFoodTitle), "The search result title does not match the expected title.");

            Console.WriteLine($"Search for '{lastCreatedFoodTitle}' returned exactly one match as expected.");
        }

        [Test, Order(5)]
        public void DeleteLastAddedFoodTest()
        {
            driver.Navigate().GoToUrl(BaseUrl);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var foodItems = wait.Until(driver => driver.FindElements(By.XPath("//div[@class='row gx-5 align-items-center']")));
           
            Assert.IsTrue(foodItems.Count > 0, "No foods were found on the page.");
            int initialFoodCount = foodItems.Count;

            var lastFoodItem = foodItems.Last();
            actions = new Actions(driver);
            actions.MoveToElement(lastFoodItem).Perform();
           
            var deleteButton = lastFoodItem.FindElement(By.XPath(".//a[contains(text(),'Delete')]"));

            actions.MoveToElement(deleteButton).Perform();
            deleteButton.Click();  

            foodItems = driver.FindElements(By.XPath("//div[@class='row gx-5 align-items-center']"));
            int finalFoodCount = foodItems.Count;
            Assert.That(finalFoodCount, Is.EqualTo(initialFoodCount - 1), "The food item count did not decrease by one after deletion.");

            if (finalFoodCount > 0)
            {
                var lastFoodTitleElement = foodItems.Last().FindElement(By.XPath(".//h2"));
                string lastFoodTitleAfterDeletion = lastFoodTitleElement.Text.Trim();
                Assert.That(lastFoodTitleAfterDeletion, Is.Not.EqualTo(lastCreatedFoodTitle), "The last food title should not match the deleted food title.");
            }

            Console.WriteLine($"Test passed: The last added food titled '{lastCreatedFoodTitle}' was successfully deleted.");

        }


        [Test, Order(6)]
        public void SearchForDeletedFoodTest()
        {
            driver.Navigate().GoToUrl(BaseUrl);

            var searchBar = driver.FindElement(By.XPath("//input[@class='form-control rounded mt-5 xl']"));
            searchBar.Clear();
            searchBar.SendKeys(lastCreatedFoodTitle);
            searchBar.SendKeys(Keys.Enter);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[@class='row gx-5 align-items-center']")));

            var noFoodsMessage = driver.FindElement(By.XPath("//h2[@class='display-4']"));
            Assert.IsTrue(noFoodsMessage.Displayed, "The 'There are no foods :(' message was not displayed as expected.");

            var addFoodButton = driver.FindElement(By.XPath("//a[@class='btn btn-primary btn-xl rounded-pill mt-5' and text()='Add food']"));
            Assert.IsTrue(addFoodButton.Displayed, "The 'Add food' button was not displayed as expected.");

            Console.WriteLine($"Test passed: The deleted food titled '{lastCreatedFoodTitle}' was not found, and the appropriate message and button were displayed.");
        }
        

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}