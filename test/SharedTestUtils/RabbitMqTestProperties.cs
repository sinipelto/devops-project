using System.Threading;
using CommonTools.Models;
using Moq;
using RabbitMQ.Client;

namespace SharedTestUtils
{
    public class RabbitMqTestProperties
    {
        public ApplicationSettings Settings { get; set; }

        public Mock<IConnection> MockConnection { get; set; }

        public Mock<IConnectionFactory> MockFactory { get; set; }

        public Mock<IModel> MockChannel { get; set; }

        public ManualResetEvent WaitHandle { get; set; }
    }
}
