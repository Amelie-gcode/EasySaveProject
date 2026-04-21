namespace EasySave.Models
{
    /// <summary>
    /// Represents the current execution status of a backup job.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// The job is configured but not currently running.
        /// </summary>
        Inactive,

        /// <summary>
        /// The job is currently executing and transferring files.
        /// </summary>
        Active,

        /// <summary>
        /// The job has finished its execution successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The job encountered a critical issue during execution.
        /// </summary>
        Error
    }
}