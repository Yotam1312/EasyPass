using EasyPass.API.Models;
using Xunit;

namespace EasyPass.Tests.Models
{
    public class UserTests
    {
        [Fact]
        public void User_LockoutFieldsHaveCorrectDefaults()
        {
            // Arrange & Act - Create a new User object
            var user = new User();

            // Assert - Check that lockout fields have correct default values
            Assert.Equal(0, user.FailedLoginCount);
            Assert.Null(user.LockoutEndAt);
            Assert.False(user.IsPermanentlyLocked);
        }
    }
}
