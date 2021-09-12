namespace CommonTools.Models
{
    /// Application service state in a shared library to ensure common understanding of the state between services.
    /// 
    /// <summary>
    /// PAUSED = ORIG service is not sending messages
    /// RUNNING = ORIG service sends messages
    /// INIT = everything is in the initial state and ORIG starts sending again
    /// SHUTDOWN = all containers are stopped
    /// </summary>
    public enum ServiceState { INIT, PAUSED, RUNNING, SHUTDOWN }
}
