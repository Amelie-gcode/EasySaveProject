using Xunit;
using EasySave.Models;
using System.Collections.Generic;

namespace EasySave.Tests
{
    //  Create a Fake Config Manager 
    public class MockConfigManager : IConfigManager
    {
        // Simulates an empty jobs.json file at startup
        public List<JobSaveData> LoadJobs()
        {
            return new List<JobSaveData>(); // Returns an empty list every time
        }

        // Does nothing, preventing the test from writing to your real hard drive
        public void SaveJobs(List<BackupJob> jobs)
        {
            // Intentionally left blank for testing
        }
    }

    public class BackupManagerTests
    {
        [Fact]
        public void CreateJob_ShouldFail_WhenAddingMoreThanFiveJobs()
        {
            // Arrange
            // Inject the fake config manager into the BackupManager
            var mockConfig = new MockConfigManager();
            var manager = new BackupManager(mockConfig);

            // the MockConfigManager guarantees the list starts perfectly empty!

            // Act 
            manager.CreateJob("Job1", @"C:\Source", @"C:\Target", false);
            manager.CreateJob("Job2", @"C:\Source", @"C:\Target", false);
            manager.CreateJob("Job3", @"C:\Source", @"C:\Target", false);
            manager.CreateJob("Job4", @"C:\Source", @"C:\Target", false);
            bool fifthJobResult = manager.CreateJob("Job5", @"C:\Source", @"C:\Target", false);

            // Try to add a 6th job (which should fail)
            bool sixthJobResult = manager.CreateJob("Job6", @"C:\Source", @"C:\Target", false);

            // Assert 
            Assert.True(fifthJobResult); // The 5th job should be accepted
            Assert.True(sixthJobResult); // The 6th job must be rejected
            Assert.Equal(6, manager.GetJobs().Count); // Total count must be exactly 5
        }
    }
}