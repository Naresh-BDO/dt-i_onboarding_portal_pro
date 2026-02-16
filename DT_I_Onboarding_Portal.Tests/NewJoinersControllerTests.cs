using System;
using System.Linq;
using System.Threading.Tasks;
using DT_I_Onboarding_Portal.Core.Models;
using DT_I_Onboarding_Portal.Core.Models.Dto;
using DT_I_Onboarding_Portal.Core.enums;
using DT_I_Onboarding_Portal.Data;
using DT_I_Onboarding_Portal.Server.Controllers;
using DT_I_Onboarding_Portal.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DT_I_Onboarding_Portal.Tests
{
    public class NewJoinersControllerTests
  {
        private static Mock<IEmailSender> CreateEmailMockSuccess()
        {
            var mock = new Mock<IEmailSender>();
            mock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(new SendEmailResult
          {
Success = true
       });

      return mock;
        }

   private static Mock<IEmailSender> CreateEmailMockFailure()
        {
     var mock = new Mock<IEmailSender>();
          mock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
     .ReturnsAsync(new SendEmailResult
       {
         Success = false,
     ErrorType = EmailErrorType.SmtpSendFailed,
       ErrorMessage = "Failed",
  ProviderMessage = "SMTP rejected"
       });

     return mock;
        }

        [Fact]
    public async Task CreateNewJoiner_ReturnsBadRequest_WhenEmailMissing()
        {
            using var db = TestDbFactory.CreateContext(nameof(CreateNewJoiner_ReturnsBadRequest_WhenEmailMissing));
var emailMock = CreateEmailMockSuccess();
    var loggerMock = new Mock<ILogger<NewJoinersController>>();
        var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

 var dto = new CreateNewJoinerDto
          {
    FullName = "John Doe",
        Email = null,
      StartDate = DateTime.UtcNow.Date
            };

    var result = await controller.CreateNewJoiner(dto);

result.Should().BeOfType<BadRequestObjectResult>();
     }

        [Fact]
  public async Task CreateNewJoiner_ReturnsConflict_WhenDuplicateEmailAndStartDate()
        {
    using var db = TestDbFactory.CreateContext(nameof(CreateNewJoiner_ReturnsConflict_WhenDuplicateEmailAndStartDate));
       db.NewJoiners.Add(new NewJoiner
       {
      FullName = "John Doe",
    Email = "john@bdo.com",
       StartDate = new DateTime(2026, 02, 01),
                CreatedAtUtc = DateTime.UtcNow
            });
         await db.SaveChangesAsync();

       var emailMock = CreateEmailMockSuccess();
            var loggerMock = new Mock<ILogger<NewJoinersController>>();
        var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

      var dto = new CreateNewJoinerDto
     {
             FullName = "John Doe",
         Email = "JOHN@BDO.COM", // should normalize and match
         StartDate = new DateTime(2026, 02, 01)
    };

var result = await controller.CreateNewJoiner(dto);

          result.Should().BeOfType<ConflictObjectResult>();
     }

        [Fact]
        public async Task CreateNewJoiner_ReturnsCreatedAtAction_WhenEmailSendSucceeds()
        {
            using var db = TestDbFactory.CreateContext(nameof(CreateNewJoiner_ReturnsCreatedAtAction_WhenEmailSendSucceeds));
         var emailMock = CreateEmailMockSuccess();
var loggerMock = new Mock<ILogger<NewJoinersController>>();
     var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

   var dto = new CreateNewJoinerDto
            {
            FullName = "  Jane Doe  ",
 Email = "  Jane@BDO.com ",
         Department = " Tax ",
                ManagerName = "Manager A",
  StartDate = new DateTime(2026, 03, 01)
            };

     var result = await controller.CreateNewJoiner(dto);

     var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(NewJoinersController.GetById));

 var saved = db.NewJoiners.Single();
         saved.Email.Should().Be("jane@bdo.com");
            saved.FullName.Should().Be("Jane Doe");
        saved.Department.Should().Be("Tax");
            saved.WelcomeEmailSentAtUtc.Should().NotBeNull();

emailMock.Verify(x => x.SendEmailAsync(
          "jane@bdo.com",
    It.IsAny<string>(),
     It.IsAny<string>()),
 Times.Once);
        }

        [Fact]
    public async Task CreateNewJoiner_ReturnsAccepted_WhenEmailSendFails_AndPersistsError()
        {
        using var db = TestDbFactory.CreateContext(nameof(CreateNewJoiner_ReturnsAccepted_WhenEmailSendFails_AndPersistsError));
            var emailMock = CreateEmailMockFailure();
   var loggerMock = new Mock<ILogger<NewJoinersController>>();
      var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

    var dto = new CreateNewJoinerDto
     {
     FullName = "John Doe",
  Email = "john@bdo.com",
                StartDate = new DateTime(2026, 03, 01)
          };

            var result = await controller.CreateNewJoiner(dto);

          result.Should().BeOfType<AcceptedResult>();

     var saved = db.NewJoiners.Single();
   saved.WelcomeEmailSentAtUtc.Should().BeNull();
      saved.LastSendError.Should().NotBeNull();
    saved.LastSendError.Should().Contain("SmtpSendFailed");
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            using var db = TestDbFactory.CreateContext(nameof(GetById_ReturnsNotFound_WhenMissing));
       var emailMock = CreateEmailMockSuccess();
        var loggerMock = new Mock<ILogger<NewJoinersController>>();
            var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

            var result = await controller.GetById(999);

          result.Should().BeOfType<NotFoundObjectResult>();
   }

        [Fact]
        public async Task DeleteNewJoiner_RemovesEntity_WhenExists()
        {
            using var db = TestDbFactory.CreateContext(nameof(DeleteNewJoiner_RemovesEntity_WhenExists));
            db.NewJoiners.Add(new NewJoiner
            {
       FullName = "Del Me",
        Email = "del@bdo.com",
         StartDate = new DateTime(2026, 02, 01),
      CreatedAtUtc = DateTime.UtcNow
   });
       await db.SaveChangesAsync();
        var id = db.NewJoiners.Single().Id;

            var emailMock = CreateEmailMockSuccess();
            var loggerMock = new Mock<ILogger<NewJoinersController>>();
            var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

        var result = await controller.DeleteNewJoiner(id);

      result.Should().BeOfType<OkObjectResult>();
  db.NewJoiners.Count().Should().Be(0);
    }

        [Fact]
    public async Task ResendWelcomeEmail_ReturnsOk_WhenSendSucceeds()
     {
       using var db = TestDbFactory.CreateContext(nameof(ResendWelcomeEmail_ReturnsOk_WhenSendSucceeds));
     db.NewJoiners.Add(new NewJoiner
      {
 FullName = "Re Send",
                Email = "resend@bdo.com",
              StartDate = new DateTime(2026, 02, 01),
        CreatedAtUtc = DateTime.UtcNow
       });
            await db.SaveChangesAsync();
          var id = db.NewJoiners.Single().Id;

        var emailMock = CreateEmailMockSuccess();
            var loggerMock = new Mock<ILogger<NewJoinersController>>();
     var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

          var result = await controller.ResendWelcomeEmail(id);

result.Should().BeOfType<OkObjectResult>();
            db.NewJoiners.Single().WelcomeEmailSentAtUtc.Should().NotBeNull();
        }

    [Fact]
        public async Task GetStatistics_ReturnsCorrectCounts()
   {
            using var db = TestDbFactory.CreateContext(nameof(GetStatistics_ReturnsCorrectCounts));
  db.NewJoiners.AddRange(
      new NewJoiner { FullName = "A", Email = "a@bdo.com", StartDate = DateTime.UtcNow.Date.AddDays(10), CreatedAtUtc = DateTime.UtcNow, WelcomeEmailSentAtUtc = DateTime.UtcNow },
      new NewJoiner { FullName = "B", Email = "b@bdo.com", StartDate = DateTime.UtcNow.Date.AddDays(-1), CreatedAtUtc = DateTime.UtcNow, LastSendError = "ProviderError: ..." },
   new NewJoiner { FullName = "C", Email = "c@bdo.com", StartDate = DateTime.UtcNow.Date, CreatedAtUtc = DateTime.UtcNow }
            );
     await db.SaveChangesAsync();

       var emailMock = CreateEmailMockSuccess();
            var loggerMock = new Mock<ILogger<NewJoinersController>>();
            var controller = new NewJoinersController(db, emailMock.Object, loggerMock.Object);

            var result = await controller.GetStatistics();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
   ok.Value.Should().NotBeNull(); // anonymous object; validate via dynamic:
       dynamic v = ok.Value!;
            ((int)v.totalNewJoiners).Should().Be(3);
((int)v.emailSent).Should().Be(1);
       ((int)v.emailFailed).Should().Be(1);
    ((int)v.upcomingJoiners).Should().Be(1);
        }
    }
}
