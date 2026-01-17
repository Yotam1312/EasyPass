using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;

namespace EasyPass.Tests.TestHelpers
{
    /// <summary>
    /// Helper class to create test databases for our tests.
    /// This makes it easy to set up clean databases for each test.
    /// </summary>
    public static class TestDatabaseHelper
    {
        /// <summary>
        /// Creates a new in-memory database for testing.
        /// Each test gets its own clean database.
        /// </summary>
        /// <param name="databaseName">Unique name for this test database</param>
        /// <returns>A DbContext ready for testing</returns>
        public static EasyPassContext CreateTestDatabase(string databaseName)
        {
            // Create options for in-memory database
            var options = new DbContextOptionsBuilder<EasyPassContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            // Create the context
            var context = new EasyPassContext(options);

            // Make sure the database is created
            context.Database.EnsureCreated();

            return context;
        }

        /// <summary>
        /// Creates a unique database name for each test.
        /// This ensures tests don't interfere with each other.
        /// </summary>
        /// <param name="testName">Name of the test</param>
        /// <returns>Unique database name</returns>
        public static string GetUniqueDatabaseName(string testName)
        {
            return $"TestDB_{testName}_{Guid.NewGuid()}";
        }
    }
}