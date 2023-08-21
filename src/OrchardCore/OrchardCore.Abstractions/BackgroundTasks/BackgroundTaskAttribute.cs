using System;

namespace OrchardCore.BackgroundTasks
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BackgroundTaskAttribute : Attribute
    {
        /// <summary>
        /// The display name of this background task.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Wether this background task is enabled or not.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// The background task schedule as a cron expression.
        /// </summary>
        public string Schedule { get; set; } = "*/5 * * * *";

        /// <summary>
        /// The description of this background task.
        /// </summary>
        public string Description { get; set; } = String.Empty;

        /// <summary>
        /// The timeout in milliseconds to acquire a lock before executing the task atomically.
        /// There is no locking if equal to zero or if there is no registered distributed lock.
        /// </summary>
        public int LockTimeout { get; set; }

        /// <summary>
        /// The expiration in milliseconds of the lock acquired before executing the task atomically.
        /// There is no locking if equal to zero or if there is no registered distributed lock.
        /// </summary>
        public int LockExpiration { get; set; }

        /// <summary>
        /// Wether or not the tenant pipeline should be built and then executed.
        /// This to configure endpoints and then to allow route urls generation.
        /// </summary>
        public bool UsePipeline { get; set; }
    }
}
