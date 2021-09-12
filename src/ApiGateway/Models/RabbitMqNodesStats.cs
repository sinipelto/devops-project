namespace ApiGateway.Models
{
    public class RabbitMqNodesStats
    {
        public ulong Uptime { get; set; }
        public uint ConnectionCreated { get; set; }
        public uint ConnectionClosed { get; set; }
        public uint ChannelCreated { get; set; }
        public uint ChannelClosed { get; set; }
        public uint QueueDeclared { get; set; }
        public uint QueueCreated { get; set; }
        public uint QueueDeleted { get; set; }
    }
}
