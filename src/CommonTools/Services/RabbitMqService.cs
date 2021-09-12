using CommonTools.Interfaces;
using CommonTools.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CommonTools.Services
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly IConnectionFactory _factory;
        private readonly ApplicationSettings _settings;
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private readonly ILogger _logger;
        private readonly Action<object, BasicDeliverEventArgs, IModel, ApplicationSettings> _receiveHandler;

        public RabbitMqService(
            IConnectionFactory factory,
            ApplicationSettings settings,
            Action<object, BasicDeliverEventArgs, IModel, ApplicationSettings> receiveHandler,
            ILogger<IRabbitMqService> logger)
        {
            _factory = factory;
            _settings = settings;
            _receiveHandler = receiveHandler;
            _logger = logger;
        }

        public RabbitMqService(
            ApplicationSettings settings,
            Action<object, BasicDeliverEventArgs, IModel, ApplicationSettings> receiveHandler,
            ILogger<IRabbitMqService> logger)
        {
            _settings = settings;
            _receiveHandler = receiveHandler;
            _factory = new ConnectionFactory { HostName = settings.RabbitMq.Host };
            _logger = logger;
        }

        public RabbitMqService(ApplicationSettings settings, ILogger<IRabbitMqService> logger)
        {
            _settings = settings;
            _receiveHandler = (a, b, c, d) => {};
            _factory = new ConnectionFactory { HostName = settings.RabbitMq.Host };
            _logger = logger;
        }

        public void StartReceiving()
        {
            InitializeConnectionIfNecessary();

            var exchange = _settings.RabbitMq.Exchange;
            var rRoutingKey = _settings.RabbitMq.ReceiveRoutingKey;
            

            var result = _channel.QueueDeclare();

            _channel.QueueBind(exchange: exchange, queue: result.QueueName, routingKey: rRoutingKey);

            _consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, ea) =>
            {
                _logger.LogDebug("Received RabbitMq message");
                _receiveHandler(model, ea, _channel, _settings);
            };

            _channel.BasicConsume(result.QueueName, true, _consumer);
        }

        public void SendMessage(string msg)
        {
            InitializeConnectionIfNecessary();

            _logger.LogDebug("Sending RabbitMq message");
            var body = Encoding.UTF8.GetBytes(msg);
            try
            {
                _channel.BasicPublish(_settings.RabbitMq.Exchange,
                        _settings.RabbitMq.SendRoutingKey,
                        null,
                        body);
            }
            catch(Exception e)
            {
                _logger.LogError($"Something went wrong while sending a RabbitMq message: {e.Message}");
            }
        }

        private void InitializeConnectionIfNecessary()
        {
            if (_connection == null || _channel == null)
            {
                _logger.LogInformation($"Initializing RabbitMq connection to exchange: {_settings.RabbitMq.Exchange}");

                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(_settings.RabbitMq.Exchange, "topic");
            }
        }

        public void TriggerDeliveryReceived(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            _consumer.HandleBasicDeliver(
                consumerTag,
                deliveryTag,
                redelivered,
                exchange,
                routingKey,
                properties,
                body);
        }
    }
}
