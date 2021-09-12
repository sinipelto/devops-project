namespace ApiGateway.Models
{
    public class RabbitMqManagementOptions
    {
        public const string Position = "RabbitMqManagement";

        public string BaseAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
