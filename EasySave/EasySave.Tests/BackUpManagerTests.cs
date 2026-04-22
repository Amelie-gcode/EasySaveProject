
using Xunit;
using EasySave.Models;

namespace EasySave.Tests
{
        public class BackupManagerTests
        {
            [Fact] // This attribute tells Visual Studio this is a test method
            public void CreateJob_ShouldFail_WhenAddingMoreThanFiveJobs()
            {
                // Arrange (Set up the environment)
                var manager = new BackupManager();

                // Act (Perform the actions)
                // Add 5 jobs (which should succeed)
                manager.CreateJob("Job1", @"C:\Source", @"C:\Target", false);
                manager.CreateJob("Job2", @"C:\Source", @"C:\Target", false);
                manager.CreateJob("Job3", @"C:\Source", @"C:\Target", false);
                manager.CreateJob("Job4", @"C:\Source", @"C:\Target", false);
                bool fifthJobResult = manager.CreateJob("Job5", @"C:\Source", @"C:\Target", false);

                // Try to add a 6th job (which should fail)
                bool sixthJobResult = manager.CreateJob("Job6", @"C:\Source", @"C:\Target", false);

                // Assert (Verify the outcome)
                Assert.True(fifthJobResult); // The 5th job should be accepted
                Assert.False(sixthJobResult); // The 6th job must be rejected
                Assert.Equal(5, manager.GetJobs().Count); // Total count must be exactly 5
            }
        }
}
