namespace CommonTools.Models
{
    public class RabbitMqSettings
    {
        public string Host { get; set; }

        public string Exchange { get; set; }

        public string ReceiveRoutingKey { get; set; }

        public string SendRoutingKey { get; set; }
    }
}