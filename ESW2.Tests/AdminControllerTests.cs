using NUnit.Framework;
using Moq;
using ESW2.Controllers;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // For TempData
using System;
using ESW2.Utilities; // For DefaultSettings

namespace ESW2.Tests
{
    [TestFixture]
    public class AdminControllerTests
    {
        private Mock<MyDbContext> _mockDbContext;
        private AdminController _controller;

        private Mock<DbSet<banco>> _mockBancoDbSet;
        private Mock<DbSet<ativo_financeiro>> _mockAtivoFinanceiroDbSet;
        private Mock<DbSet<deposito_prazo>> _mockDepositoPrazoDbSet;
        private Mock<DbSet<utilizador_cliente>> _mockUtilizadorClienteDbSet;
        private Mock<DbSet<utilizador>> _mockUtilizadorDbSet; // For user counts in Stats

        private Mock<HttpContext> _mockHttpContext;
        private ClaimsPrincipal _adminUser;

        private double _originalInterestRate;
        private double _originalTaxRate;

        [SetUp]
        public void Setup()
        {
            // Backup static DefaultSettings
            _originalInterestRate = DefaultSettings.CurrentInterestRate;
            _originalTaxRate = DefaultSettings.CurrentTaxRate;

            _mockDbContext = new Mock<MyDbContext>(new DbContextOptions<MyDbContext>());

            _mockBancoDbSet = new Mock<DbSet<banco>>();
            _mockAtivoFinanceiroDbSet = new Mock<DbSet<ativo_financeiro>>();
            _mockDepositoPrazoDbSet = new Mock<DbSet<deposito_prazo>>();
            _mockUtilizadorClienteDbSet = new Mock<DbSet<utilizador_cliente>>();
            _mockUtilizadorDbSet = new Mock<DbSet<utilizador>>();

            _mockDbContext.Setup(c => c.bancos).Returns(_mockBancoDbSet.Object);
            _mockDbContext.Setup(c => c.ativo_financeiros).Returns(_mockAtivoFinanceiroDbSet.Object);
            _mockDbContext.Setup(c => c.deposito_prazos).Returns(_mockDepositoPrazoDbSet.Object);
            _mockDbContext.Setup(c => c.utilizador_clientes).Returns(_mockUtilizadorClienteDbSet.Object);
            _mockDbContext.Setup(c => c.utilizadors).Returns(_mockUtilizadorDbSet.Object);

            SetupMockDbSet(_mockBancoDbSet, new List<banco>());
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>());
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo>());
            SetupMockDbSet(_mockUtilizadorClienteDbSet, new List<utilizador_cliente>());
            SetupMockDbSet(_mockUtilizadorDbSet, new List<utilizador>());

            _mockHttpContext = new Mock<HttpContext>();
            _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mockAdmin"));
            _mockHttpContext.Setup(hc => hc.User).Returns(_adminUser);

            _controller = new AdminController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                },
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        [TearDown]
        public void Teardown()
        {
            // Restore static DefaultSettings
            DefaultSettings.CurrentInterestRate = _originalInterestRate;
            DefaultSettings.CurrentTaxRate = _originalTaxRate;
        }

        // Helper to setup mock DbSet (can be reused from other test files if in a shared location)
        public static void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, List<T> sourceList) where T : class
        {
            var queryableList = sourceList.AsQueryable();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryableList.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableList.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableList.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryableList.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryableList.GetEnumerator()));

            mockSet.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, token) => sourceList.Add(entity))
                .ReturnsAsync((T entity, CancellationToken token) => Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>)null); // Simplified, actual type is EntityEntry<T>

            mockSet.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<T>, CancellationToken>((entities, token) => sourceList.AddRange(entities))
                .Returns(Task.CompletedTask);

            mockSet.Setup(s => s.Remove(It.IsAny<T>()))
                .Callback<T>(entity => sourceList.Remove(entity));

            mockSet.Setup(s => s.RemoveRange(It.IsAny<IEnumerable<T>>()))
                .Callback<IEnumerable<T>>(entities => {
                    foreach (var entity in entities.ToList()) sourceList.Remove(entity);
                });
        }

        // Test methods will be added here
        // --- Dashboard Tests ---
        [Test]
        public void Dashboard_RedirectsToIndex()
        {
            // Act
            var result = _controller.Dashboard() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        // --- Index Tests ---
        [Test]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        // --- Banks (GET) Tests ---
        [Test]
        public void Banks_ReturnsViewResult_WithListOfBanks()
        {
            // Arrange
            var banks = new List<banco>
            {
                new banco { id_banco = 1, nome_banco = "Banco B" },
                new banco { id_banco = 2, nome_banco = "Banco A" }
            };
            // The controller uses ToList(), so IQueryable setup is enough.
            var mockSet = new Mock<DbSet<banco>>();
            mockSet.As<IQueryable<banco>>().Setup(m => m.Provider).Returns(banks.AsQueryable().Provider);
            mockSet.As<IQueryable<banco>>().Setup(m => m.Expression).Returns(banks.AsQueryable().Expression);
            mockSet.As<IQueryable<banco>>().Setup(m => m.ElementType).Returns(banks.AsQueryable().ElementType);
            mockSet.As<IQueryable<banco>>().Setup(m => m.GetEnumerator()).Returns(() => banks.GetEnumerator());
            _mockDbContext.Setup(c => c.bancos).Returns(mockSet.Object);


            // Act
            var result = _controller.Banks() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as List<banco>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
            Assert.AreEqual("Banco A", model[0].nome_banco); // Check ordering
        }

        // --- AddBank (POST) Tests ---
        [Test]
        public async Task AddBank_Post_ReturnsView_WhenBankNameIsEmpty()
        {
            // Arrange
            _controller.ModelState.AddModelError("nome_banco", "O nome do banco é obrigatório."); // Manually add error as controller would
            var existingBanks = new List<banco>();
            SetupMockDbSet(_mockBancoDbSet, existingBanks); // For the return View("Banks", banks)

            // Act
            var result = await _controller.AddBank(null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ViewName); // Should return to Banks view
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("nome_banco"));
        }

        [Test]
        public async Task AddBank_Post_ReturnsView_WhenBankNameExists()
        {
            // Arrange
            var existingBanks = new List<banco> { new banco { nome_banco = "Existing Bank" } };
            SetupMockDbSet(_mockBancoDbSet, existingBanks);
            // _controller.ModelState.AddModelError("nome_banco", "Já existe um banco com este nome."); // Controller adds this

            // Act
            var result = await _controller.AddBank("Existing Bank") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ViewName);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("nome_banco"));
            Assert.AreEqual("Já existe um banco com este nome.", result.ViewData.ModelState["nome_banco"].Errors[0].ErrorMessage);
        }

        [Test]
        public async Task AddBank_Post_AddsBankAndRedirectsToBanks_WhenValid()
        {
            // Arrange
            var banksList = new List<banco>(); // To check AddAsync
            SetupMockDbSet(_mockBancoDbSet, banksList);
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.AddBank("New Valid Bank") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ActionName);
            _mockBancoDbSet.Verify(m => m.AddAsync(It.Is<banco>(b => b.nome_banco == "New Valid Bank"), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(1, banksList.Count(b => b.nome_banco == "New Valid Bank"));
        }

        // --- EditBank (GET) Tests ---
        [Test]
        public void EditBank_Get_ReturnsNotFound_WhenBankNotFound()
        {
            // Arrange
            _mockBancoDbSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((banco)null);

            // Act
            var result = _controller.EditBank(1) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void EditBank_Get_ReturnsViewResult_WithBank_WhenFound()
        {
            // Arrange
            var bank = new banco { id_banco = 1, nome_banco = "Test Bank" };
            _mockBancoDbSet.Setup(m => m.Find(1)).Returns(bank);

            // Act
            var result = _controller.EditBank(1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as banco;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.id_banco);
        }

        // --- EditBank (POST) Tests ---
        [Test]
        public async Task EditBank_Post_ReturnsNotFound_WhenBankNotFound()
        {
            // Arrange
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync((banco)null);

            // Act
            var result = await _controller.EditBank(1, "New Name") as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task EditBank_Post_ReturnsView_WhenBankNameIsEmpty()
        {
            // Arrange
            var bank = new banco { id_banco = 1, nome_banco = "Original Name" };
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync(bank);
            // Controller will add ModelState error

            // Act
            var result = await _controller.EditBank(1, "") as ViewResult; // Empty name

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("nome_banco"));
            Assert.AreEqual(bank, result.Model); // Should return the original bank model to view
        }

        [Test]
        public async Task EditBank_Post_UpdatesBankAndRedirectsToBanks_WhenValid()
        {
            // Arrange
            var bank = new banco { id_banco = 1, nome_banco = "Original Name" };
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync(bank);
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.EditBank(1, "Updated Name") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ActionName);
            Assert.AreEqual("Updated Name", bank.nome_banco); // Verify the name was updated on the tracked entity
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- DeleteBank (GET/POST) Tests ---
        [Test]
        public async Task DeleteBank_Get_DeletesBankAndRedirectsToBanks_WhenBankFound()
        {
            // Arrange
            var bank = new banco { id_banco = 1, nome_banco = "Bank To Delete" };
            var banksList = new List<banco> { bank };
            SetupMockDbSet(_mockBancoDbSet, banksList); // So FindAsync can find it via the DbSet mock
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync(bank); // Explicitly mock FindAsync
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteBank(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ActionName);
            _mockBancoDbSet.Verify(m => m.Remove(bank), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual("Banco excluído com sucesso!", _controller.TempData["SuccessMessage"]);
        }

        [Test]
        public async Task DeleteBank_Get_RedirectsToBanks_WhenBankNotFound()
        {
            // Arrange
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync((banco)null); // Bank not found

            // Act
            var result = await _controller.DeleteBank(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Banks", result.ActionName);
            _mockBancoDbSet.Verify(m => m.Remove(It.IsAny<banco>()), Times.Never); // Ensure Remove is not called
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never); // Ensure SaveChanges is not called
            Assert.IsNull(_controller.TempData["SuccessMessage"]); // No success message
        }

        [Test] // This is essentially the same as the first DeleteBank test, but explicitly checks TempData
        public async Task DeleteBank_Get_SetsSuccessMessage_WhenBankDeleted()
        {
            // Arrange
            var bank = new banco { id_banco = 1, nome_banco = "Bank To Delete" };
            SetupMockDbSet(_mockBancoDbSet, new List<banco> { bank });
            _mockDbContext.Setup(c => c.bancos.FindAsync(1)).ReturnsAsync(bank);
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _controller.DeleteBank(1);

            // Assert
            Assert.AreEqual("Banco excluído com sucesso!", _controller.TempData["SuccessMessage"]);
        }

        // --- Settings (GET) Tests ---
        [Test]
        public void Settings_Get_ReturnsViewResult_WithAdminSettingsViewModel()
        {
            // Arrange
            DefaultSettings.CurrentInterestRate = 7.5M; // Set a specific value for the test
            DefaultSettings.CurrentTaxRate = 20.0M;

            // Act
            var result = _controller.Settings() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as AdminSettingsViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(7.5M, model.DefaultInterestRate);
            Assert.AreEqual(20.0M, model.DefaultTaxRate);
        }

        // --- UpdateSettings (POST) Tests ---
        [Test]
        public void UpdateSettings_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("DefaultInterestRate", "Error");
            var viewModel = new AdminSettingsViewModel { DefaultInterestRate = -1, DefaultTaxRate = 10 }; // Invalid model

            // Act
            var result = _controller.UpdateSettings(viewModel) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Settings", result.ViewName); // Should return to Settings view
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.AreEqual(viewModel, result.Model); // Model should be passed back
        }

        [Test]
        public void UpdateSettings_Post_UpdatesDefaultSettingsAndRedirectsToSettings_WhenValid()
        {
            // Arrange
            var viewModel = new AdminSettingsViewModel { DefaultInterestRate = 8.0M, DefaultTaxRate = 22.0M };

            // Act
            var result = _controller.UpdateSettings(viewModel) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Settings", result.ActionName);
            Assert.AreEqual(8.0M, DefaultSettings.CurrentInterestRate);
            Assert.AreEqual(22.0M, DefaultSettings.CurrentTaxRate);
            Assert.AreEqual("Configurações atualizadas com sucesso!", _controller.TempData["SuccessMessage"]);
        }

        [Test] // This is somewhat redundant with the above but explicitly checks TempData
        public void UpdateSettings_Post_SetsSuccessMessage_WhenSettingsUpdated()
        {
            // Arrange
            var viewModel = new AdminSettingsViewModel { DefaultInterestRate = 9.0M, DefaultTaxRate = 25.0M };

            // Act
            _controller.UpdateSettings(viewModel);

            // Assert
            Assert.AreEqual("Configurações atualizadas com sucesso!", _controller.TempData["SuccessMessage"]);
            Assert.AreEqual(9.0M, DefaultSettings.CurrentInterestRate); // Also verify values were set
            Assert.AreEqual(25.0M, DefaultSettings.CurrentTaxRate);
        }

        // --- RelatorioBancos Tests ---
        [Test]
        public void RelatorioBancos_Get_ReturnsViewResult_WithReportData_WhenDatesProvided()
        {
            // Arrange
            var dataInicio = new DateTime(2023, 1, 1);
            var dataFim = new DateTime(2023, 1, 31);

            var banco1 = new banco { id_banco = 1, nome_banco = "Banco X" };
            var deposito1 = new deposito_prazo { id_deposito = 1, id_banco = 1, valor_deposito = 1000, taxa_juro_anual = 1.0, id_bancoNavigation = banco1 };
            var ativo1 = new ativo_financeiro { id_ativo = 1, id_deposito = 1, data_inicio = DateOnly.FromDateTime(new DateTime(2023,1,15)), id_depositoNavigation = deposito1 };

            var banco2 = new banco { id_banco = 2, nome_banco = "Banco Y" };
            var deposito2 = new deposito_prazo { id_deposito = 2, id_banco = 2, valor_deposito = 2000, taxa_juro_anual = 1.2, id_bancoNavigation = banco2 };
            var ativo2 = new ativo_financeiro { id_ativo = 2, id_deposito = 2, data_inicio = DateOnly.FromDateTime(new DateTime(2023,1,10)), id_depositoNavigation = deposito2 };

            // Ativo outside date range
            var deposito3 = new deposito_prazo { id_deposito = 3, id_banco = 1, valor_deposito = 500, taxa_juro_anual = 1.0, id_bancoNavigation = banco1 };
            var ativo3 = new ativo_financeiro { id_ativo = 3, id_deposito = 3, data_inicio = DateOnly.FromDateTime(new DateTime(2023,2,1)), id_depositoNavigation = deposito3 };


            SetupMockDbSet(_mockBancoDbSet, new List<banco> { banco1, banco2 });
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo> { deposito1, deposito2, deposito3 });
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo1, ativo2, ativo3 });

            // Act
            var result = _controller.RelatorioBancos(dataInicio, dataFim) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("RelatorioBancos", result.ViewName);
            var model = result.Model as List<dynamic>; // The controller returns List<AnonymousType>
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count); // Only banco1 and banco2 should have ativos in range

            dynamic relatorioBancoX = model.First(r => r.Banco == "Banco X");
            Assert.AreEqual(1000.0, relatorioBancoX.TotalDeposito);
            Assert.AreEqual(1000.0 * (1.0/100.0), relatorioBancoX.CustoTotalJuros);

            dynamic relatorioBancoY = model.First(r => r.Banco == "Banco Y");
            Assert.AreEqual(2000.0, relatorioBancoY.TotalDeposito);
            Assert.AreEqual(2000.0 * (1.2/100.0), relatorioBancoY.CustoTotalJuros);

            Assert.AreEqual(dataInicio.ToString("yyyy-MM-dd"), result.ViewData["DataInicio"]);
            Assert.AreEqual(dataFim.ToString("yyyy-MM-dd"), result.ViewData["DataFim"]);
        }

        [Test]
        public void RelatorioBancos_Get_ReturnsViewResult_WithDefaultDateData_WhenDatesNotProvided()
        {
            // Arrange - No specific data needed as we're checking default date behavior
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo>());
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>());

            // Act
            var result = _controller.RelatorioBancos(null, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as List<dynamic>;
            Assert.IsNotNull(model);
            Assert.IsEmpty(model); // No data, so list should be empty

            Assert.AreEqual(DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd"), result.ViewData["DataInicio"]);
            Assert.AreEqual(DateTime.Today.ToString("yyyy-MM-dd"), result.ViewData["DataFim"]);
        }

        [Test]
        public void RelatorioBancos_Get_CorrectlyFiltersData_BasedOnDateRange()
        {
            // Arrange
            var dataInicioFiltro = new DateTime(2023, 2, 1);
            var dataFimFiltro = new DateTime(2023, 2, 28);

            var banco1 = new banco { id_banco = 1, nome_banco = "Banco A" };
            // Ativo DENTRO do range de fevereiro
            var deposito1 = new deposito_prazo { id_deposito = 1, id_banco = 1, valor_deposito = 100, taxa_juro_anual = 1, id_bancoNavigation = banco1 };
            var ativo1 = new ativo_financeiro { id_ativo = 1, id_deposito = 1, data_inicio = DateOnly.FromDateTime(new DateTime(2023, 2, 15)), id_depositoNavigation = deposito1 };

            // Ativo FORA do range (janeiro)
            var deposito2 = new deposito_prazo { id_deposito = 2, id_banco = 1, valor_deposito = 200, taxa_juro_anual = 1, id_bancoNavigation = banco1 };
            var ativo2 = new ativo_financeiro { id_ativo = 2, id_deposito = 2, data_inicio = DateOnly.FromDateTime(new DateTime(2023, 1, 15)), id_depositoNavigation = deposito2 };

            SetupMockDbSet(_mockBancoDbSet, new List<banco> { banco1 });
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo> { deposito1, deposito2 });
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo1, ativo2 });

            // Act
            var result = _controller.RelatorioBancos(dataInicioFiltro, dataFimFiltro) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as List<dynamic>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count); // Only Banco A should have data, and only from ativo1
            Assert.AreEqual("Banco A", model[0].Banco);
            Assert.AreEqual(100.0, model[0].TotalDeposito);
        }

        // --- Stats Tests ---
        [Test]
        public async Task Stats_Get_ReturnsViewResult_WithDashboardStatsViewModel()
        {
            // Arrange
            // Default empty setups from Setup() are fine for just checking view model type

            // Act
            var result = await _controller.Stats() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as DashboardStatsViewModel;
            Assert.IsNotNull(model);
        }

        [Test]
        public async Task Stats_Get_CalculatesStatsCorrectly()
        {
            // Arrange
            var clientes = new List<utilizador_cliente>
            {
                new utilizador_cliente { id_cliente = 1 },
                new utilizador_cliente { id_cliente = 2 }
            };
            SetupMockDbSet(_mockUtilizadorClienteDbSet, clientes);

            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1 },
                new ativo_financeiro { id_ativo = 2 },
                new ativo_financeiro { id_ativo = 3 }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);

            var depositos = new List<deposito_prazo>
            {
                new deposito_prazo { id_deposito = 1, valor_deposito = 1000.50 },
                new deposito_prazo { id_deposito = 2, valor_deposito = 2000.25 },
                new deposito_prazo { id_deposito = 3, valor_deposito = 500.00 }
            };
            SetupMockDbSet(_mockDepositoPrazoDbSet, depositos);

            // Act
            var result = await _controller.Stats() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as DashboardStatsViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.TotalClients);
            Assert.AreEqual(3, model.TotalAssets);
            Assert.AreEqual(1000.50 + 2000.25 + 500.00, model.TotalValueManaged);
        }
    }
}
