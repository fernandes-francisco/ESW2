using NUnit.Framework;
using Moq;
using ESW2.Controllers;
using ESW2.Data; // Assuming MyDbContext is in ESW2.Data
using ESW2.Models; // Assuming utilizador and other models are in ESW2.Models
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using System.Threading;
using Microsoft.AspNetCore.Identity; // For SignInManager and UserManager if used

namespace ESW2.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        // Mock fields will be added here in later steps
        private Mock<MyDbContext> _mockDbContext;
        private Mock<ILogger<AccountController>> _mockLogger;
        private AccountController _controller;
        private Mock<DbSet<utilizador>> _mockUserDbSet;
        private Mock<IAuthenticationService> _mockAuthService;
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mock<HttpContext> _mockHttpContext;
        // private Mock<SignInManager<utilizador>> _mockSignInManager; // Not used by Controller
        // private Mock<UserManager<utilizador>> _mockUserManager; // Not used by Controller


        [SetUp]
        public void Setup()
        {
            // Initialization of mocks will be done here
            _mockLogger = new Mock<ILogger<AccountController>>();

            // Mocking DbContext and DbSet
            _mockDbContext = new Mock<MyDbContext>(new DbContextOptions<MyDbContext>());
            _mockUserDbSet = new Mock<DbSet<utilizador>>();

            var users = new List<utilizador>().AsQueryable();

            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
            _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));

            // Corrected DbSet name to 'utilizadors' as per MyDbContext.cs
            _mockDbContext.Setup(c => c.utilizadors).Returns(_mockUserDbSet.Object);
            // _mockDbContext.Setup(c => c.Set<utilizador>()).Returns(_mockUserDbSet.Object); // Redundant if utilizadors is directly mocked


            // UserManager and SignInManager are not used by the AccountController directly.
            // Authentication is handled via HttpContext.SignInAsync/SignOutAsync with CookieAuthenticationDefaults.

            // Mock HttpContext & Authentication
            _mockHttpContext = new Mock<HttpContext>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(_mockAuthService.Object);
            _mockHttpContext.Setup(hc => hc.RequestServices).Returns(_mockServiceProvider.Object);

            // Setup User for HttpContext
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "test"));
            _mockHttpContext.Setup(hc => hc.User).Returns(claimsPrincipal);


            _controller = new AccountController(_mockDbContext.Object, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        // Test methods will be added here

        [Test]
        public async Task Login_Post_ReturnsView_WhenModelIsInvalid_UsernameEmpty()
        {
            // Arrange
            _controller.ModelState.AddModelError("username", "Required"); // Simulate model state error

            // Act
            var result = await _controller.Login(null, "somepassword") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Por favor, preencha todos os campos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Login_Post_ReturnsView_WhenModelIsInvalid_PasswordEmpty()
        {
            // Arrange
             // Controller directly checks for empty strings, not ModelState for this specific case in the provided code
            // No need to add ModelState error here, the controller logic handles it.

            // Act
            var result = await _controller.Login("someuser", "") as ViewResult; // Empty password

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Por favor, preencha todos os campos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Login_Post_ReturnsView_WhenModelIsInvalid_BothEmpty()
        {
            // Arrange
            // Controller directly checks for empty strings

            // Act
            var result = await _controller.Login("", "") as ViewResult; // Empty username and password

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Por favor, preencha todos os campos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Login_Post_ReturnsView_WhenCredentialsAreInvalid()
        {
            // Arrange
            var users = new List<utilizador>().AsQueryable();
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
             _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
            _mockDbContext.Setup(c => c.utilizadors).Returns(_mockUserDbSet.Object);


            // Act
            var result = await _controller.Login("wronguser", "wrongpassword") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Nome de utilizador ou palavra-passe incorretos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Login_Post_RedirectsToAdminDashboard_WhenAdminLogsIn()
        {
            // Arrange
            var adminUser = new utilizador { id_utilizador = 1, username = "admin", password = "password", is_admin = true };
            var users = new List<utilizador> { adminUser }.AsQueryable();

            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
            _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
             _mockDbContext.Setup(db => db.utilizadors).Returns(_mockUserDbSet.Object);

            _mockAuthService.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login("admin", "password") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Dashboard", result.ActionName);
            Assert.AreEqual("Admin", result.ControllerName);
            _mockAuthService.Verify(a => a.SignInAsync(
                _mockHttpContext.Object,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(cp => cp.Identity.Name == "admin" && cp.IsInRole("Admin")),
                It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        [Test]
        public async Task Login_Post_RedirectsToAtivoFinanceiroIndex_WhenClienteLogsIn()
        {
            // Arrange
            var clientUser = new utilizador { id_utilizador = 2, username = "cliente", password = "password", is_admin = false };
            var users = new List<utilizador> { clientUser }.AsQueryable();

            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
             _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
            _mockDbContext.Setup(db => db.utilizadors).Returns(_mockUserDbSet.Object);

            _mockAuthService.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login("cliente", "password") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("AtivoFinanceiro", result.ControllerName);
             _mockAuthService.Verify(a => a.SignInAsync(
                _mockHttpContext.Object,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(cp => cp.Identity.Name == "cliente" && cp.IsInRole("Cliente")),
                It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        [Test]
        public async Task Login_Post_RedirectsToReturnUrl_WhenClienteLogsInAndReturnUrlIsValid()
        {
            // Arrange
            var clientUser = new utilizador { id_utilizador = 3, username = "clienteurl", password = "password", is_admin = false };
            var users = new List<utilizador> { clientUser }.AsQueryable();
             _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
             _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
            _mockDbContext.Setup(db => db.utilizadors).Returns(_mockUserDbSet.Object);

            _mockAuthService.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);
            _controller.Url = mockUrlHelper.Object;

            string returnUrl = "/SomeController/SomeAction";

            // Act
            var result = await _controller.Login("clienteurl", "password", returnUrl) as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);
             _mockAuthService.Verify(a => a.SignInAsync(
                _mockHttpContext.Object,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(cp => cp.Identity.Name == "clienteurl" && cp.IsInRole("Cliente")),
                It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        // --- Register Tests ---
        [Test]
        public async Task Register_Post_ReturnsView_WhenModelIsInvalid_MissingUsername()
        {
            // Arrange
            // Controller checks forIsNullOrWhiteSpace directly

            // Act
            var result = await _controller.Register("", "test@example.com", "password", "123456789", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Nome de utilizador, email e senha são obrigatórios.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenModelIsInvalid_MissingEmail()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "", "password", "123456789", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Nome de utilizador, email e senha são obrigatórios.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenModelIsInvalid_MissingPassword()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "", "123456789", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Nome de utilizador, email e senha são obrigatórios.", result.ViewData["ErrorMessage"]);
        }


        [Test]
        public async Task Register_Post_ReturnsView_WhenNifIsInvalid_TooShort()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "password", "12345", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("O NIF é obrigatório e deve ter exatamente 9 dígitos numéricos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenNifIsInvalid_NotNumeric()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "password", "abcdefghi", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("O NIF é obrigatório e deve ter exatamente 9 dígitos numéricos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenNifIsInvalid_Empty()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "password", "", "Some Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("O NIF é obrigatório e deve ter exatamente 9 dígitos numéricos.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenMoradaIsInvalid_Empty()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "password", "123456789", "") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("A morada é obrigatória e não pode ter mais de 200 caracteres.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_ReturnsView_WhenMoradaIsInvalid_TooLong()
        {
            // Arrange
            // Act
            var result = await _controller.Register("newuser", "test@example.com", "password", "123456789", new string('a', 201)) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("A morada é obrigatória e não pode ter mais de 200 caracteres.", result.ViewData["ErrorMessage"]);
        }


        [Test]
        public async Task Register_Post_ReturnsView_WhenUsernameExists()
        {
            // Arrange
            var existingUser = new utilizador { id_utilizador = 1, username = "existinguser", password = "password", email = "existing@example.com", is_admin = false };
            var users = new List<utilizador> { existingUser }.AsQueryable();
             _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
             _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
            _mockDbContext.Setup(db => db.utilizadors).Returns(_mockUserDbSet.Object);

            // Act
            var result = await _controller.Register("existinguser", "new@example.com", "newpass", "123456780", "New Address") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Este nome de utilizador já está em uso.", result.ViewData["ErrorMessage"]);
        }

        [Test]
        public async Task Register_Post_RedirectsToLogin_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var users = new List<utilizador>().AsQueryable(); // No existing user with the new username
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<utilizador>(users.Provider));
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserDbSet.As<IQueryable<utilizador>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
             _mockUserDbSet.As<IAsyncEnumerable<utilizador>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<utilizador>(users.GetEnumerator()));
            _mockDbContext.Setup(db => db.utilizadors).Returns(_mockUserDbSet.Object);

            _mockUserDbSet.Setup(m => m.AddAsync(It.IsAny<utilizador>(), It.IsAny<CancellationToken>()))
                .Callback<utilizador, CancellationToken>((u, ct) => users.ToList().Add(u)); // Simulate adding to list

            var mockClientDbSet = new Mock<DbSet<utilizador_cliente>>();
            _mockDbContext.Setup(db => db.utilizador_clientes).Returns(mockClientDbSet.Object);
             mockClientDbSet.Setup(m => m.AddAsync(It.IsAny<utilizador_cliente>(), It.IsAny<CancellationToken>()));


            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); // Simulate successful save

            // Mock transaction
            var mockTransaction = new Mock<IDbContextTransaction>();
            _mockDbContext.Setup(db => db.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockTransaction.Object);

            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());


            // Act
            var result = await _controller.Register("newuser", "new@example.com", "password", "987654321", "Valid Address") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Registo realizado com sucesso! Faça login para continuar.", _controller.TempData["SuccessMessage"]);
            _mockUserDbSet.Verify(m => m.AddAsync(It.Is<utilizador>(u => u.username == "newuser"), It.IsAny<CancellationToken>()), Times.Once);
            mockClientDbSet.Verify(m => m.AddAsync(It.Is<utilizador_cliente>(uc => uc.nif == "987654321"), It.IsAny<CancellationToken>()), Times.Once);
            _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for user, once for client profile
            mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- Logout Test ---
        [Test]
        public async Task Logout_RedirectsToLogin()
        {
            // Arrange
            _mockAuthService.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            _mockAuthService.Verify(a => a.SignOutAsync(
                _mockHttpContext.Object,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()), Times.Once);
        }
    }

    // Helper classes for mocking IAsyncEnumerable and IQueryable
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                     name: nameof(IQueryProvider.Execute),
                                     genericParameterCount: 1,
                                     types: new[] { typeof(Expression) })
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                        .MakeGenericMethod(expectedResultType)
                                        .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }
    }
}
