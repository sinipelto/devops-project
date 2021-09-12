namespace CommonTools.Models
{
    public class ApplicationSettings
    {
        public RabbitMqSettings RabbitMq { get; set; }

        public string MsgLogFile { get; set; }
    }
}