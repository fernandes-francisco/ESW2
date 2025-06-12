using NUnit.Framework;
using Moq;
using ESW2.Controllers;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models; // Assuming ViewModels like AtivoFinanceiroViewModel are here
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Only if AtivoFinanceiroController uses it
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // For TempData
using System;

namespace ESW2.Tests
{
    [TestFixture]
    public class AtivoFinanceiroControllerTests
    {
        private Mock<MyDbContext> _mockDbContext;
        private AtivoFinanceiroController _controller;

        private Mock<DbSet<ativo_financeiro>> _mockAtivoFinanceiroDbSet;
        private Mock<DbSet<utilizador_cliente>> _mockUtilizadorClienteDbSet;
        private Mock<DbSet<utilizador>> _mockUtilizadorDbSet; // For GetCurrentClienteId
        private Mock<DbSet<banco>> _mockBancoDbSet;
        private Mock<DbSet<deposito_prazo>> _mockDepositoPrazoDbSet;
        private Mock<DbSet<fundo_investimento>> _mockFundoInvestimentoDbSet;
        private Mock<DbSet<imovel_arrendado>> _mockImovelArrendadoDbSet;
        private Mock<DbSet<pagamento_imposto>> _mockPagamentoImpostoDbSet; // For delete checks

        private Mock<HttpContext> _mockHttpContext;
        private ClaimsPrincipal _user; // Authenticated user

        private const int TestUserId = 1;
        private const int TestClienteId = 10;
        private const string TestUsername = "testuser";

        [SetUp]
        public void Setup()
        {
            _mockDbContext = new Mock<MyDbContext>(new DbContextOptions<MyDbContext>());

            // Initialize DbSets
            _mockAtivoFinanceiroDbSet = new Mock<DbSet<ativo_financeiro>>();
            _mockUtilizadorClienteDbSet = new Mock<DbSet<utilizador_cliente>>();
            _mockUtilizadorDbSet = new Mock<DbSet<utilizador>>();
            _mockBancoDbSet = new Mock<DbSet<banco>>();
            _mockDepositoPrazoDbSet = new Mock<DbSet<deposito_prazo>>();
            _mockFundoInvestimentoDbSet = new Mock<DbSet<fundo_investimento>>();
            _mockImovelArrendadoDbSet = new Mock<DbSet<imovel_arrendado>>();
            _mockPagamentoImpostoDbSet = new Mock<DbSet<pagamento_imposto>>();

            // Setup DbContext to return mocked DbSets
            _mockDbContext.Setup(c => c.ativo_financeiros).Returns(_mockAtivoFinanceiroDbSet.Object);
            _mockDbContext.Setup(c => c.utilizador_clientes).Returns(_mockUtilizadorClienteDbSet.Object);
            _mockDbContext.Setup(c => c.utilizadors).Returns(_mockUtilizadorDbSet.Object); // Corrected from Utilizadores
            _mockDbContext.Setup(c => c.bancos).Returns(_mockBancoDbSet.Object);
            _mockDbContext.Setup(c => c.deposito_prazos).Returns(_mockDepositoPrazoDbSet.Object);
            _mockDbContext.Setup(c => c.fundo_investimentos).Returns(_mockFundoInvestimentoDbSet.Object);
            _mockDbContext.Setup(c => c.imovel_arrendados).Returns(_mockImovelArrendadoDbSet.Object);
            _mockDbContext.Setup(c => c.pagamento_impostos).Returns(_mockPagamentoImpostoDbSet.Object);

            // Mock HttpContext and ClaimsPrincipal
            _mockHttpContext = new Mock<HttpContext>();
            _user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, TestUsername),
                new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()) // This is typically user ID from 'utilizador' table
            }, "mock"));
            _mockHttpContext.Setup(hc => hc.User).Returns(_user);

            // Mock GetCurrentClienteId behavior
            // The controller gets username from User.Identity.Name, then finds 'utilizador', then 'utilizador_cliente'
            var testUtilizador = new utilizador { id_utilizador = TestUserId, username = TestUsername };
            var utilizadoresList = new List<utilizador> { testUtilizador };
            SetupMockDbSet(_mockUtilizadorDbSet, utilizadoresList);

            var testCliente = new utilizador_cliente { id_cliente = TestClienteId, id_utilizador = TestUserId, nif = "123456789", morada = "Test Address" };
            var clientesList = new List<utilizador_cliente> { testCliente };
            SetupMockDbSet(_mockUtilizadorClienteDbSet, clientesList);

            // Setup empty lists for other DbSets by default, tests can override
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>());
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo>());
            SetupMockDbSet(_mockFundoInvestimentoDbSet, new List<fundo_investimento>());
            SetupMockDbSet(_mockImovelArrendadoDbSet, new List<imovel_arrendado>());
            SetupMockDbSet(_mockPagamentoImpostoDbSet, new List<pagamento_imposto>());


            // Initialize controller
            _controller = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                },
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        // Helper to setup mock DbSet
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
                .ReturnsAsync((T entity, CancellationToken token) => null); // Return type for AddAsync is EntityEntry<T>

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

        // Test methods for Index action will be added here
        [Test]
        public async Task Index_ReturnsViewResult_WithListOfAtivos_ForAuthenticatedUser()
        {
            // Arrange
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>()); // For ViewBag

            // Act
            var result = await _controller.Index(null, null, null, null, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public async Task Index_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No authenticated user

            // Re-initialize controller with the new HttpContext mock that has no user
             var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };


            // Act
            var result = await controllerNoUser.Index(null, null, null, null, null) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado ou utilizador não autenticado.", result.Value);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnNomeParameter_DepositoNomeBanco()
        {
            // Arrange
            var banco1 = new banco { id_banco = 1, nome_banco = "BancoAlpha" };
            var deposito1 = new deposito_prazo { id_deposito = 1, id_banco = 1, id_bancoNavigation = banco1, numero_conta_banco = "111" };
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito1, data_inicio = DateOnly.FromDateTime(DateTime.Today)},
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, id_fundoNavigation = new fundo_investimento{nome = "FundoBeta"}, data_inicio = DateOnly.FromDateTime(DateTime.Today)}
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>{ banco1 });

            // Act
            var result = await _controller.Index("Alpha", null, null, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(1, model.First().id_ativo);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnNomeParameter_DepositoNumeroConta()
        {
            // Arrange
            var banco1 = new banco { id_banco = 1, nome_banco = "BancoAlpha" };
            var deposito1 = new deposito_prazo { id_deposito = 1, id_banco = 1, id_bancoNavigation = banco1, numero_conta_banco = "ACC111" };
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito1, data_inicio = DateOnly.FromDateTime(DateTime.Today)},
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, id_fundoNavigation = new fundo_investimento{nome = "FundoBeta"}, data_inicio = DateOnly.FromDateTime(DateTime.Today)}
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>{ banco1 });

            // Act
            var result = await _controller.Index("ACC111", null, null, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(1, model.First().id_ativo);
        }


        [Test]
        public async Task Index_FiltersAtivos_BasedOnTipoParameter_Deposito()
        {
            // Arrange
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, "Deposito", null, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.IsTrue(model.First().id_deposito.HasValue);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnTipoParameter_Fundo()
        {
            // Arrange
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, "Fundo", null, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.IsTrue(model.First().id_fundo.HasValue);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnTipoParameter_Imovel()
        {
            // Arrange
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, id_imovel = 1, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, "Imovel", null, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.IsTrue(model.First().id_imovel.HasValue);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnMontanteAplicadoParameter()
        {
            // Arrange
            var deposito1 = new deposito_prazo { id_deposito = 1, valor_deposito = 1000 };
            var fundo1 = new fundo_investimento { id_fundo = 1, valor_investido = 2000 };
            var deposito2 = new deposito_prazo { id_deposito = 2, valor_deposito = 1000 }; // Another with 1000

            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, id_fundoNavigation = fundo1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, id_deposito = 2, id_depositoNavigation = deposito2, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            // The controller queries these DbSets directly for the filter
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo> { deposito1, deposito2 });
            SetupMockDbSet(_mockFundoInvestimentoDbSet, new List<fundo_investimento> { fundo1 });
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());


            // Act
            var result = await _controller.Index(null, null, 1000, null, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count); // ativo 1 and ativo 3
            Assert.IsTrue(model.All(a => a.id_depositoNavigation?.valor_deposito == 1000));
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnBancosParameter()
        {
            // Arrange
            var banco1 = new banco { id_banco = 1, nome_banco = "Banco A" };
            var banco2 = new banco { id_banco = 2, nome_banco = "Banco B" };
            var deposito1 = new deposito_prazo { id_deposito = 1, id_banco = 1, id_bancoNavigation = banco1 };
            var deposito2 = new deposito_prazo { id_deposito = 2, id_banco = 2, id_bancoNavigation = banco2 };
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito1, data_inicio = DateOnly.FromDateTime(DateTime.Today) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_deposito = 2, id_depositoNavigation = deposito2, data_inicio = DateOnly.FromDateTime(DateTime.Today) }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco> { banco1, banco2 });


            // Act
            var result = await _controller.Index(null, null, null, null, new int[] { 1 }) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(1, model.First().id_depositoNavigation.id_banco);
        }

        [Test]
        public async Task Index_FiltersAtivos_BasedOnSomenteAtivosAtuaisParameter_True()
        {
            // Arrange
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var ativos = new List<ativo_financeiro>
            {
                // Ativo (ends in future)
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, data_inicio = hoje.AddMonths(-1), duracao_meses = 3, estado = estado_ativo.Ativo },
                // Inativo (but would end in future if active)
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, data_inicio = hoje.AddMonths(-1), duracao_meses = 3, estado = estado_ativo.Inativo },
                // Ativo (ends in past)
                new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, data_inicio = hoje.AddMonths(-6), duracao_meses = 3, estado = estado_ativo.Ativo },
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, null, null, true, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(1, model.First().id_ativo);
        }

        [Test]
        public async Task Index_OrdersAtivos_ByValorInicial_WhenSomenteAtivosAtuaisIsTrue()
        {
            // Arrange
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var deposito1 = new deposito_prazo { id_deposito = 1, valor_deposito = 1000 };
            var fundo1 = new fundo_investimento { id_fundo = 1, valor_investido = 2000 };
            var imovel1 = new imovel_arrendado { id_imovel = 1, valor_imovel = 500 };

            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito1, data_inicio = hoje.AddMonths(-1), duracao_meses = 2, estado = estado_ativo.Ativo },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, id_fundoNavigation = fundo1, data_inicio = hoje.AddMonths(-1), duracao_meses = 2, estado = estado_ativo.Ativo },
                new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, id_imovel = 1, id_imovelNavigation = imovel1, data_inicio = hoje.AddMonths(-1), duracao_meses = 2, estado = estado_ativo.Ativo },
            };
             // Ensure the list is pre-shuffled to test ordering
            var shuffledAtivos = ativos.OrderBy(a => Guid.NewGuid()).ToList();
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, shuffledAtivos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, null, null, true, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count);
            Assert.AreEqual(2, model[0].id_ativo); // Fundo (2000)
            Assert.AreEqual(1, model[1].id_ativo); // Deposito (1000)
            Assert.AreEqual(3, model[2].id_ativo); // Imovel (500)
        }

        [Test]
        public async Task Index_OrdersAtivos_ByDataInicio_WhenSomenteAtivosAtuaisIsFalseOrNull()
        {
            // Arrange
             var hoje = DateOnly.FromDateTime(DateTime.Today);
            var ativos = new List<ativo_financeiro>
            {
                new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, data_inicio = hoje.AddDays(-10) },
                new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, data_inicio = hoje.AddDays(-5) },
                new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, data_inicio = hoje.AddDays(-20) },
            };
            var shuffledAtivos = ativos.OrderBy(a => Guid.NewGuid()).ToList();
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, shuffledAtivos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Index(null, null, null, false, null) as ViewResult;

            // Assert
            var model = result?.Model as List<ativo_financeiro>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count);
            Assert.AreEqual(2, model[0].id_ativo); // Most recent
            Assert.AreEqual(1, model[1].id_ativo);
            Assert.AreEqual(3, model[2].id_ativo); // Oldest
        }

        // --- Details Tests ---
        [Test]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Details(null) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ID do ativo não fornecido.", result.Value);
        }

        [Test]
        public async Task Details_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object) // New controller instance with modified HttpContext
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData // Preserve TempData if needed by other parts of action
            };

            // Act
            var result = await controllerNoUser.Details(1) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado.", result.Value);
        }

        [Test]
        public async Task Details_ReturnsNotFound_WhenAtivoNotFound()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos

            // Act
            var result = await _controller.Details(1) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Ativo financeiro com ID 1 não encontrado.", result.Value);
        }

        [Test]
        public async Task Details_ReturnsUnauthorized_WhenUserTriesToAccessAnotherUsersAtivo()
        {
            // Arrange
            var otherClienteId = TestClienteId + 1;
            var ativo = new ativo_financeiro { id_ativo = 1, id_cliente = otherClienteId }; // Belongs to another client
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });

            // Act
            var result = await _controller.Details(1) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Não tem permissão para visualizar os detalhes deste ativo.", result.Value);
        }

        [Test]
        public async Task Details_ReturnsViewResult_WithAtivoFinanceiro_WhenFoundAndAuthorized()
        {
            // Arrange
            var ativo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });

            // Act
            var result = await _controller.Details(1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as ativo_financeiro;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.id_ativo);
        }

        // --- Create (GET) Tests ---
        [Test]
        public async Task Create_Get_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
             var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };

            // Act
            var result = await controllerNoUser.Create(null) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado para criar ativo.", result.Value);
        }

        [Test]
        public async Task Create_Get_ReturnsViewResult_WithViewModelAndLoadsViewBagData()
        {
            // Arrange
            var bancos = new List<banco> { new banco { id_banco = 1, nome_banco = "Test Banco" } };
            var depositos = new List<deposito_prazo> { new deposito_prazo { id_deposito = 1, numero_conta_banco = "123", id_bancoNavigation = bancos.First() } };
            SetupMockDbSet(_mockBancoDbSet, bancos);
            SetupMockDbSet(_mockDepositoPrazoDbSet, depositos);
            // Ensure other DbSets for ReloadCreateViewData are set up, even if empty, if controller tries to access them
            SetupMockDbSet(_mockFundoInvestimentoDbSet, new List<fundo_investimento>());
            SetupMockDbSet(_mockImovelArrendadoDbSet, new List<imovel_arrendado>());


            // Act
            var result = await _controller.Create(null) as ViewResult; // tipoAtivo = null defaults to "Deposito"

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as ativo_financeiro;
            Assert.IsNotNull(model);
            Assert.AreEqual(estado_ativo.Ativo, model.estado); // Default state
            Assert.AreEqual(DateOnly.FromDateTime(DateTime.Today), model.data_inicio); // Default date

            Assert.AreEqual("Deposito", result.ViewData["TipoAtivo"]);
            Assert.IsNotNull(result.ViewData["Bancos"]);
            Assert.IsNotNull(result.ViewData["Depositos"]);
            Assert.AreEqual(1, (result.ViewData["Bancos"] as List<banco>)?.Count);
            Assert.AreEqual(1, (result.ViewData["Depositos"] as List<deposito_prazo>)?.Count);
        }

        [Test]
        public async Task Create_Get_LoadsViewBagData_ForFundo()
        {
            // Arrange
            var fundos = new List<fundo_investimento> { new fundo_investimento { id_fundo = 1, nome = "Test Fundo" } };
            SetupMockDbSet(_mockFundoInvestimentoDbSet, fundos);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>()); // Still need bancos for general layout
             // Ensure other DbSets for ReloadCreateViewData are set up
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo>());
            SetupMockDbSet(_mockImovelArrendadoDbSet, new List<imovel_arrendado>());

            // Act
            var result = await _controller.Create("Fundo") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Fundo", result.ViewData["TipoAtivo"]);
            Assert.IsNotNull(result.ViewData["Fundos"]);
            Assert.AreEqual(1, (result.ViewData["Fundos"] as List<fundo_investimento>)?.Count);
        }

        [Test]
        public async Task Create_Get_LoadsViewBagData_ForImovel()
        {
            // Arrange
            var imoveis = new List<imovel_arrendado> { new imovel_arrendado { id_imovel = 1, designacao = "Test Imovel" } };
            SetupMockDbSet(_mockImovelArrendadoDbSet, imoveis);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());
            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo>());
            SetupMockDbSet(_mockFundoInvestimentoDbSet, new List<fundo_investimento>());


            // Act
            var result = await _controller.Create("Imovel") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Imovel", result.ViewData["TipoAtivo"]);
            Assert.IsNotNull(result.ViewData["Imoveis"]);
            Assert.AreEqual(1, (result.ViewData["Imoveis"] as List<imovel_arrendado>)?.Count);
        }

        // --- Create (POST) Tests ---
        [Test]
        public async Task Create_Post_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };
            var ativoModel = new ativo_financeiro();

            // Act
            var result = await controllerNoUser.Create(ativoModel, "Deposito", 1, "123", "Titular", 1000, 1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey(""));
            Assert.AreEqual("Não foi possível identificar o cliente. Inicie sessão novamente.", result.ViewData.ModelState[""]?.Errors.First().ErrorMessage);
            // Check if ReloadCreateViewData was called by verifying ViewBag properties (indirectly)
            Assert.IsNotNull(result.ViewData["Bancos"]);
        }

        [Test]
        public async Task Create_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("duracao_meses", "Error"); // Simulate ModelState error
            var ativoModel = new ativo_financeiro();
             SetupMockDbSet(_mockBancoDbSet, new List<banco>()); // For ReloadCreateViewData

            // Act
            var result = await _controller.Create(ativoModel, "Deposito", 1, "123", "Titular", 1000, 1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.AreEqual(ativoModel, result.Model);
            Assert.IsNotNull(result.ViewData["Bancos"]); // Check if ReloadCreateViewData was called
        }

        [Test]
        public async Task Create_Post_CreatesNewDepositoAndAtivo_AndReturnsViewWithSuccess_WhenTipoAtivoIsDepositoAndNewDeposito()
        {
            // Arrange
            var ativoModel = new ativo_financeiro { data_inicio = DateOnly.FromDateTime(DateTime.Today), duracao_meses = 12, percentual_imposto = 10, estado = estado_ativo.Ativo };
            var bancos = new List<banco> { new banco { id_banco = 1, nome_banco = "Banco Teste" } };
            var depositosSource = new List<deposito_prazo>(); // To capture added deposito

            SetupMockDbSet(_mockBancoDbSet, bancos);
            SetupMockDbSet(_mockDepositoPrazoDbSet, depositosSource); // Pass the list to capture AddAsync
             _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); // Simulate successful save for new deposito and then for ativo

            // Act
            var result = await _controller.Create(ativoModel, "Deposito", 1, "Conta123", "Titular Teste", 5000, 2.5) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.IsValid);
             Assert.AreEqual("Ativo financeiro (Depósito) inserido com sucesso!", result.ViewData["SuccessMessage"]);

            _mockDepositoPrazoDbSet.Verify(d => d.AddAsync(It.Is<deposito_prazo>(dp => dp.numero_conta_banco == "Conta123"), It.IsAny<CancellationToken>()), Times.Once);
            _mockAtivoFinanceiroDbSet.Verify(a => a.AddAsync(It.Is<ativo_financeiro>(af => af.id_cliente == TestClienteId && af.id_deposito != null), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for new deposito, once for ativo
        }

        [Test]
        public async Task Create_Post_UsesExistingDepositoAndCreatesAtivo_AndReturnsViewWithSuccess_WhenTipoAtivoIsDepositoAndExistingDeposito()
        {
            // Arrange
            var existingDeposito = new deposito_prazo { id_deposito = 5, id_banco = 1, numero_conta_banco = "Existing123", valor_deposito = 3000 };
            var ativoModel = new ativo_financeiro { id_deposito = 5, data_inicio = DateOnly.FromDateTime(DateTime.Today), duracao_meses = 6, percentual_imposto = 5 };

            SetupMockDbSet(_mockDepositoPrazoDbSet, new List<deposito_prazo> { existingDeposito });
            _mockDbContext.Setup(c => c.deposito_prazos.FindAsync(5)).ReturnsAsync(existingDeposito); // Ensure FindAsync works if used by controller logic for existing checks
            _mockDbContext.Setup(c => c.deposito_prazos.AnyAsync(It.IsAny<Expression<Func<deposito_prazo, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<deposito_prazo, bool>> predicate, CancellationToken token) =>
                    new List<deposito_prazo>{ existingDeposito }.AsQueryable().Any(predicate));


            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>()); // For Reload

            // Act
            var result = await _controller.Create(ativoModel, "Deposito") as ViewResult; // No new deposito fields passed

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.IsValid);
            Assert.AreEqual("Ativo financeiro (Depósito) inserido com sucesso!", result.ViewData["SuccessMessage"]);
            _mockAtivoFinanceiroDbSet.Verify(a => a.AddAsync(It.Is<ativo_financeiro>(af => af.id_deposito == 5 && af.id_cliente == TestClienteId), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // Only for ativo
        }

        [Test]
        public async Task Create_Post_CreatesNewFundoAndAtivo_AndReturnsViewWithSuccess_WhenTipoAtivoIsFundoAndNewFundo()
        {
            // Arrange
            var ativoModel = new ativo_financeiro { data_inicio = DateOnly.FromDateTime(DateTime.Today), duracao_meses = 24, percentual_imposto = 15 };
            var fundosSource = new List<fundo_investimento>();
            SetupMockDbSet(_mockFundoInvestimentoDbSet, fundosSource);
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>()); // For Reload

            // Act
            var result = await _controller.Create(ativoModel, "Fundo", null, null, null, null, null, "Fundo Novo Alpha", 10000, 5.0) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.IsValid);
            Assert.AreEqual("Ativo financeiro (Fundo) inserido com sucesso!", result.ViewData["SuccessMessage"]);
             _mockFundoInvestimentoDbSet.Verify(f => f.AddAsync(It.Is<fundo_investimento>(fi => fi.nome == "Fundo Novo Alpha"), It.IsAny<CancellationToken>()), Times.Once);
            _mockAtivoFinanceiroDbSet.Verify(a => a.AddAsync(It.Is<ativo_financeiro>(af => af.id_cliente == TestClienteId && af.id_fundo != null), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Create_Post_CreatesNewImovelAndAtivo_AndReturnsViewWithSuccess_WhenTipoAtivoIsImovelAndNewImovel()
        {
            // Arrange
            var ativoModel = new ativo_financeiro { data_inicio = DateOnly.FromDateTime(DateTime.Today), duracao_meses = 120, percentual_imposto = 20 };
            var imoveisSource = new List<imovel_arrendado>();
            SetupMockDbSet(_mockImovelArrendadoDbSet, imoveisSource);
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());

            // Act
            var result = await _controller.Create(ativoModel, "Imovel",
                null, null, null, null, null, null, null, null,
                "Apartamento T2 Centro", "Rua Principal 123", 150000, 750, 50, 200) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.IsValid);
            Assert.AreEqual("Ativo financeiro (Imóvel) inserido com sucesso!", result.ViewData["SuccessMessage"]);
            _mockImovelArrendadoDbSet.Verify(i => i.AddAsync(It.Is<imovel_arrendado>(im => im.designacao == "Apartamento T2 Centro"), It.IsAny<CancellationToken>()), Times.Once);
            _mockAtivoFinanceiroDbSet.Verify(a => a.AddAsync(It.Is<ativo_financeiro>(af => af.id_cliente == TestClienteId && af.id_imovel != null), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Create_Post_HandlesDbUpdateException_AndReturnsView()
        {
            // Arrange
            var ativoModel = new ativo_financeiro { data_inicio = DateOnly.FromDateTime(DateTime.Today), duracao_meses = 12, percentual_imposto = 10 };
            SetupMockDbSet(_mockBancoDbSet, new List<banco>());
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new DbUpdateException("Test DB update error", new Exception("Inner test error")));

            // Act
            // Attempt to create a new deposito which will trigger SaveChangesAsync for the deposito first
            var result = await _controller.Create(ativoModel, "Deposito", 1, "ContaDBError", "Titular", 100, 1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("")); // Error added to general model state
            Assert.IsTrue(result.ViewData.ModelState[""]?.Errors.Any(e => e.ErrorMessage.Contains("Erro ao criar o novo Deposito")));
        }

        // --- Edit (GET) Tests ---
        [Test]
        public async Task Edit_Get_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Edit(null) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ID do ativo não fornecido.", result.Value);
        }

        [Test]
        public async Task Edit_Get_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };

            // Act
            var result = await controllerNoUser.Edit(1) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado.", result.Value);
        }

        [Test]
        public async Task Edit_Get_ReturnsNotFound_WhenAtivoNotFound()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos in DB

            // Act
            var result = await _controller.Edit(1) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Ativo financeiro com ID 1 não encontrado.", result.Value);
        }

        [Test]
        public async Task Edit_Get_ReturnsUnauthorized_WhenUserTriesToAccessAnotherUsersAtivo()
        {
            // Arrange
            var otherClienteId = TestClienteId + 1;
            var ativoToEdit = new ativo_financeiro { id_ativo = 1, id_cliente = otherClienteId }; // Belongs to another client
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativoToEdit });

            // Act
            var result = await _controller.Edit(1) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Não tem permissão para atualizar este ativo.", result.Value);
        }

        [Test]
        public async Task Edit_Get_ReturnsViewResult_WithAtivoFinanceiro_WhenFoundAndAuthorized()
        {
            // Arrange
            var ativoToEdit = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativoToEdit });

            // Act
            var result = await _controller.Edit(1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as ativo_financeiro;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.id_ativo);
        }

        // --- Edit (POST) Tests ---
        [Test]
        public async Task Edit_Post_ReturnsBadRequest_WhenIdDoesNotMatchAtivoId()
        {
            // Arrange
            var ativoModel = new ativo_financeiro { id_ativo = 2 }; // Different ID in model

            // Act
            var result = await _controller.Edit(1, ativoModel) as BadRequestObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Inconsistência no ID do ativo.", result.Value);
        }

        [Test]
        public async Task Edit_Post_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };
            var ativoModel = new ativo_financeiro { id_ativo = 1 };

            // Act
            var result = await controllerNoUser.Edit(1, ativoModel) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado.", result.Value);
        }

        [Test]
        public async Task Edit_Post_ReturnsNotFound_WhenAtivoNotFound()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos
            var ativoModel = new ativo_financeiro { id_ativo = 1 };

            // Act
            var result = await _controller.Edit(1, ativoModel) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Ativo financeiro com ID 1 não encontrado para atualização.", result.Value);
        }

        [Test]
        public async Task Edit_Post_ReturnsUnauthorized_WhenUserTriesToAccessAnotherUsersAtivo()
        {
            // Arrange
            var otherClienteId = TestClienteId + 1;
            var originalAtivo = new ativo_financeiro { id_ativo = 1, id_cliente = otherClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { originalAtivo });
            var ativoModelFromForm = new ativo_financeiro { id_ativo = 1 }; // Form model

            // Act
            var result = await _controller.Edit(1, ativoModelFromForm) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Não tem permissão para atualizar este ativo.", result.Value);
        }

        [Test]
        public async Task Edit_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var originalAtivo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
             SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { originalAtivo });
            _controller.ModelState.AddModelError("duracao_meses", "Error");
            var ativoModelFromForm = new ativo_financeiro { id_ativo = 1 };


            // Act
            var result = await _controller.Edit(1, ativoModelFromForm) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.AreEqual(ativoModelFromForm, result.Model); // Should return the model from form
        }

        [Test]
        public async Task Edit_Post_UpdatesAtivo_AndRedirectsToIndex_WhenValid()
        {
            // Arrange
            var originalAtivo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, duracao_meses = 6, percentual_imposto = 5 };
            var ativosList = new List<ativo_financeiro> { originalAtivo }; // The list that SetupMockDbSet uses
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativosList);

            var ativoModelFromForm = new ativo_financeiro
            {
                id_ativo = 1,
                data_inicio = originalAtivo.data_inicio, // Assuming these are not changed or are set by controller
                estado = estado_ativo.Inativo, // Changed state
                duracao_meses = 12, // Changed duration
                percentual_imposto = 10 // Changed tax
            };

            _mockDbContext.Setup(c => c.Update(It.IsAny<ativo_financeiro>()));
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.Edit(1, ativoModelFromForm) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Ativo financeiro atualizado com sucesso!", _controller.TempData["SuccessMessage"]);

            _mockDbContext.Verify(c => c.Update(It.Is<ativo_financeiro>(a =>
                a.id_ativo == 1 &&
                a.estado == estado_ativo.Inativo &&
                a.duracao_meses == 12 &&
                a.percentual_imposto == 10 &&
                a.id_cliente == TestClienteId // Ensure client ID is preserved
            )), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Edit_Post_HandlesDbUpdateConcurrencyException_WhenAtivoNotFoundDuringUpdate()
        {
            // Arrange
            var originalAtivo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { originalAtivo }); // Ativo exists initially

            var ativoModelFromForm = new ativo_financeiro { id_ativo = 1, duracao_meses = 10, percentual_imposto = 10 };

            _mockDbContext.Setup(c => c.Update(It.IsAny<ativo_financeiro>()));
            // Simulate SaveChangesAsync throwing concurrency exception
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new DbUpdateConcurrencyException());
            // Simulate ativo_financeiroExists returning false (it was deleted by another user)
            _mockDbContext.Setup(db => db.ativo_financeiros.Any(It.IsAny<Expression<Func<ativo_financeiro, bool>>>()))
                           .Returns(false); // Mocking Any for exists check.

            // Act
            var result = await _controller.Edit(1, ativoModelFromForm) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Ativo foi apagado enquanto editava.", result.Value);
        }

        [Test]
        public async Task Edit_Post_HandlesDbUpdateConcurrencyException_WhenConcurrencyConflict()
        {
            // Arrange
            var originalAtivo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { originalAtivo });

            var ativoModelFromForm = new ativo_financeiro { id_ativo = 1, duracao_meses = 10, percentual_imposto = 10 };

            _mockDbContext.Setup(c => c.Update(It.IsAny<ativo_financeiro>()));
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new DbUpdateConcurrencyException());
            // Simulate ativo_financeiroExists returning true (it still exists, but was modified)
             _mockAtivoFinanceiroDbSet.Setup(db => db.Any(It.IsAny<Expression<Func<ativo_financeiro, bool>>>()))
                           .Returns(true); // Mocking Any for exists check.
             _mockDbContext.Setup(db => db.ativo_financeiros).Returns(_mockAtivoFinanceiroDbSet.Object);


            // Act
            var result = await _controller.Edit(1, ativoModelFromForm) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState[""]?.Errors.Any(e => e.ErrorMessage == "Este ativo foi modificado. Recarregue a página e tente novamente."));
        }

        // --- Delete (GET) Tests ---
        [Test]
        public async Task Delete_Get_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Delete(null) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ID do ativo não fornecido.", result.Value);
        }

        [Test]
        public async Task Delete_Get_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
             var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };
            // Act
            var result = await controllerNoUser.Delete(1) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado.", result.Value);
        }

        [Test]
        public async Task Delete_Get_ReturnsRedirectToIndex_WithInfoMsg_WhenAtivoNotFound()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos

            // Act
            var result = await _controller.Delete(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual($"Ativo financeiro com ID 1 não encontrado ou já foi apagado.", _controller.TempData["InfoMessage"]);
        }

        [Test]
        public async Task Delete_Get_ReturnsRedirectToIndex_WithErrorMsg_WhenUserTriesToAccessAnotherUsersAtivo()
        {
            // Arrange
            var otherClienteId = TestClienteId + 1;
            var ativoToDelete = new ativo_financeiro { id_ativo = 1, id_cliente = otherClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativoToDelete });

            // Act
            var result = await _controller.Delete(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Não tem permissão para apagar este ativo.", _controller.TempData["ErrorMessage"]);
        }

        [Test]
        public async Task Delete_Get_ReturnsViewResult_WithAtivoFinanceiro_WhenFoundAndAuthorized()
        {
            // Arrange
            var ativoToDelete = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativoToDelete });

            // Act
            var result = await _controller.Delete(1) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as ativo_financeiro;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.id_ativo);
        }

        // --- DeleteConfirmed (POST) Tests ---
        [Test]
        public async Task DeleteConfirmed_Post_ReturnsRedirectToIndex_WithErrorMsg_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };
            // Act
            var result = await controllerNoUser.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Perfil de cliente não encontrado.", controllerNoUser.TempData["ErrorMessage"]);
        }

        [Test]
        public async Task DeleteConfirmed_Post_ReturnsRedirectToIndex_WithInfoMsg_WhenAtivoNotFoundOrUnauthorized()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos, or not the user's

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Ativo financeiro não encontrado ou você não tem permissão para apagá-lo.", _controller.TempData["InfoMessage"]);
        }

        [Test]
        public async Task DeleteConfirmed_Post_ReturnsRedirectToIndex_WithErrorMsg_WhenAtivoHasRelatedPagamentoImpostos()
        {
            // Arrange
            var ativo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId };
            var impostos = new List<pagamento_imposto> { new pagamento_imposto { id_imposto = 1, id_ativo = 1 } };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });
            SetupMockDbSet(_mockPagamentoImpostoDbSet, impostos);

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Não é possível apagar este ativo porque ele possui impostos pagos associados.", _controller.TempData["ErrorMessage"]);
        }

        [Test]
        public async Task DeleteConfirmed_Post_DeletesAtivoAndAssociatedEntities_AndRedirectsToIndex_WhenValid_Deposito()
        {
            // Arrange
            var deposito = new deposito_prazo { id_deposito = 10, numero_conta_banco = "D1" };
            var ativo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 10, id_depositoNavigation = deposito };

            var ativosList = new List<ativo_financeiro> { ativo };
            var depositosList = new List<deposito_prazo> { deposito };

            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativosList);
            SetupMockDbSet(_mockDepositoPrazoDbSet, depositosList);
            SetupMockDbSet(_mockPagamentoImpostoDbSet, new List<pagamento_imposto>()); // No impostos

            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Ativo financeiro apagado com sucesso!", _controller.TempData["SuccessMessage"]);
            _mockAtivoFinanceiroDbSet.Verify(m => m.Remove(It.Is<ativo_financeiro>(a => a.id_ativo == 1)), Times.Once);
            _mockDepositoPrazoDbSet.Verify(m => m.Remove(It.Is<deposito_prazo>(d => d.id_deposito == 10)), Times.Once);
            _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteConfirmed_Post_DeletesAtivoAndAssociatedEntities_AndRedirectsToIndex_WhenValid_Fundo()
        {
            // Arrange
            var fundo = new fundo_investimento { id_fundo = 20, nome = "F1" };
            var ativo = new ativo_financeiro { id_ativo = 2, id_cliente = TestClienteId, id_fundo = 20, id_fundoNavigation = fundo };

            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });
            SetupMockDbSet(_mockFundoInvestimentoDbSet, new List<fundo_investimento> { fundo });
            SetupMockDbSet(_mockPagamentoImpostoDbSet, new List<pagamento_imposto>());

            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(2) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            _mockFundoInvestimentoDbSet.Verify(m => m.Remove(It.Is<fundo_investimento>(f => f.id_fundo == 20)), Times.Once);
        }

        [Test]
        public async Task DeleteConfirmed_Post_DeletesAtivoAndAssociatedEntities_AndRedirectsToIndex_WhenValid_Imovel()
        {
            // Arrange
            var imovel = new imovel_arrendado { id_imovel = 30, designacao = "I1" };
            var ativo = new ativo_financeiro { id_ativo = 3, id_cliente = TestClienteId, id_imovel = 30, id_imovelNavigation = imovel };

            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });
            SetupMockDbSet(_mockImovelArrendadoDbSet, new List<imovel_arrendado> { imovel });
            SetupMockDbSet(_mockPagamentoImpostoDbSet, new List<pagamento_imposto>());

            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(3) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            _mockImovelArrendadoDbSet.Verify(m => m.Remove(It.Is<imovel_arrendado>(i => i.id_imovel == 30)), Times.Once);
        }


        [Test]
        public async Task DeleteConfirmed_Post_HandlesDbUpdateException()
        {
            // Arrange
            var ativo = new ativo_financeiro { id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1 };
             SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro> { ativo });
            SetupMockDbSet(_mockPagamentoImpostoDbSet, new List<pagamento_imposto>()); // No impostos

            _mockDbContext.Setup(c => c.Remove(It.IsAny<ativo_financeiro>()));
            _mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new DbUpdateException("Test DB error", new Exception("Inner test error")));

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Erro ao apagar o ativo ou registo associado: Inner test error", _controller.TempData["ErrorMessage"]);
        }

        // --- RelatorioLucro Tests ---
        [Test]
        public async Task RelatorioLucro_ReturnsUnauthorized_WhenClienteIdIsNull()
        {
            // Arrange
            _mockHttpContext.Setup(hc => hc.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // No user
            var controllerNoUser = new AtivoFinanceiroController(_mockDbContext.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _mockHttpContext.Object },
                TempData = _controller.TempData
            };

            // Act
            var result = await controllerNoUser.RelatorioLucro(DateTime.Now.AddMonths(-1), DateTime.Now) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Perfil de cliente não encontrado.", result.Value);
        }

        [Test]
        public async Task RelatorioLucro_ReturnsView_WithCorrectData_WhenDateRangeIsValid()
        {
            // Arrange
            var dataInicioRelatorio = DateTime.Now.AddMonths(-2);
            var dataFimRelatorio = DateTime.Now;

            var deposito = new deposito_prazo { id_deposito = 1, valor_deposito = 10000, taxa_juro_anual = 1.2 }; // 1.2% per year = 0.1% per month
            var fundo = new fundo_investimento { id_fundo = 1, valor_investido = 5000, taxa_juro_padrao = 2.4 }; // 2.4% per year = 0.2% per month

            var ativos = new List<ativo_financeiro>
            {
                // Deposito ativo for 2 full months within range
                new ativo_financeiro {
                    id_ativo = 1, id_cliente = TestClienteId, id_deposito = 1, id_depositoNavigation = deposito,
                    data_inicio = DateOnly.FromDateTime(dataInicioRelatorio), duracao_meses = 3, percentual_imposto = 10
                },
                // Fundo ativo for 1 full month and part of another within range (assuming controller logic handles partial months by counting full months in range)
                 new ativo_financeiro {
                    id_ativo = 2, id_cliente = TestClienteId, id_fundo = 1, id_fundoNavigation = fundo,
                    data_inicio = DateOnly.FromDateTime(dataInicioRelatorio.AddDays(15)), duracao_meses = 2, percentual_imposto = 20
                }
            };
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, ativos);

            // Act
            var result = await _controller.RelatorioLucro(dataInicioRelatorio, dataFimRelatorio) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as RelatorioLucroViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(dataInicioRelatorio, model.DataInicio);
            Assert.AreEqual(dataFimRelatorio, model.DataFim);
            Assert.AreEqual(2, model.Linhas.Count);

            // Deposito: 10000 * 0.012 / 12 = 10 bruto/mes. 2 meses = 20 bruto. 10% imposto = 18 liquido total.
            var linhaDeposito = model.Linhas.FirstOrDefault(l => l.NomeAtivo.Contains("Depósito"));
            Assert.IsNotNull(linhaDeposito);
            Assert.AreEqual(10m * 2, linhaDeposito.LucroTotalBruto); // Controller logic counts 2 months
            Assert.AreEqual(10m * (1m - 0.10m) * 2, linhaDeposito.LucroTotalLiquido);

            // Fundo: 5000 * 0.024 / 12 = 10 bruto/mes. Controller counts months from start of ativo within range.
            // If data_inicio is 15th of month1, and duracao is 2 months, it ends 15th of month3.
            // Range: month1 to month2. Controller logic for 'meses' in loop might be simple month count.
            // For dataInicioRelatorio to dataFimRelatorio (2 months duration for report)
            // Ativo 2 (Fundo): starts 15/M1, ends 15/M3.
            // Intersection with report range (M1-M2): From 15/M1 to EndOf(M2).
            // Controller's 'meses' calc: ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month + 1
            // where inicio = max(dataInicioRelatorio, data_inicio_efetiva)
            // and fim = min(dataFimRelatorio, data_fim_efetiva)
            // Fundo: data_inicio_efetiva = dataInicioRelatorio.AddDays(15). data_fim_efetiva = data_inicio_efetiva.AddMonths(2)
            // inicio = dataInicioRelatorio.AddDays(15)
            // fim = dataFimRelatorio (if dataFimRelatorio < data_fim_efetiva)
            // Example: Report M1-M2. Fundo starts 15/M1, ends 15/M3.
            // inicio_calc = 15/M1. fim_calc = EndOf(M2). meses = (M2-M1)+1 = 2.
            var linhaFundo = model.Linhas.FirstOrDefault(l => l.NomeAtivo.Contains("Fundo"));
            Assert.IsNotNull(linhaFundo);
            Assert.AreEqual(10m * 2, linhaFundo.LucroTotalBruto); // Controller logic counts 2 months
            Assert.AreEqual(10m * (1m - 0.20m) * 2, linhaFundo.LucroTotalLiquido);
        }

        [Test]
        public async Task RelatorioLucro_ReturnsView_WithEmptyData_WhenDateRangeIsNotValidOrNoAtivos()
        {
            // Arrange
            SetupMockDbSet(_mockAtivoFinanceiroDbSet, new List<ativo_financeiro>()); // No ativos

            // Act - No date range, should return empty
            var resultNoDate = await _controller.RelatorioLucro(null, null) as ViewResult;
            // Act - Date range but no ativos
            var resultWithDateNoAtivos = await _controller.RelatorioLucro(DateTime.Now.AddMonths(-1), DateTime.Now) as ViewResult;


            // Assert No Date
            Assert.IsNotNull(resultNoDate);
            var modelNoDate = resultNoDate.Model as RelatorioLucroViewModel;
            Assert.IsNotNull(modelNoDate);
            Assert.IsFalse(modelNoDate.DataInicio.HasValue);
            Assert.IsFalse(modelNoDate.DataFim.HasValue);
            Assert.IsEmpty(modelNoDate.Linhas);

            // Assert Date Range No Ativos
            Assert.IsNotNull(resultWithDateNoAtivos);
            var modelWithDateNoAtivos = resultWithDateNoAtivos.Model as RelatorioLucroViewModel;
            Assert.IsNotNull(modelWithDateNoAtivos);
            Assert.IsTrue(modelWithDateNoAtivos.DataInicio.HasValue);
            Assert.IsTrue(modelWithDateNoAtivos.DataFim.HasValue);
            Assert.IsEmpty(modelWithDateNoAtivos.Linhas);
        }
    }

    // Re-use helper classes from AccountControllerTests if they are not globally accessible
    // If they are in the same namespace (ESW2.Tests), they should be available.
    // Otherwise, copy them here or make them public in a shared file.
    // Assuming TestAsyncQueryProvider, TestAsyncEnumerable, TestAsyncEnumerator are available.
}
