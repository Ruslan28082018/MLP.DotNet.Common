using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace API.V1.RPC
{
    //
    // Summary:
    //     Provides a Reply Part of Request-Reply Pattern
    //
    // Example:
    // using (aReply = new RpcServer())
    // {
    //   // handle rcieved message
    //   aReply.HandlerReceivedJson = (message) => message;
    // )
    public class RpcServer : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;

        /// <summary>
        /// Received Handler
        /// new RpcServer().ReceivedJsonHandler = (message) => message;
        /// </summary>
        /// <param name="message">json to process</param>
        /// <returns>json result</returns>
        public Func<string, string> HandlerReceivedJson { get; set; }

        private readonly string _hostName;
        private readonly string _queueName;

        public RpcServer(string aHostName = "localhost", string aQueueName = "rpc_queue")
        {
            _hostName = aHostName;
            _queueName = aQueueName;

            var channelInstanceRes = CreateChannel(_hostName, _queueName);
            _connection = channelInstanceRes.connection;
            _channel = channelInstanceRes.channel;

            _consumer = CreateConsumer(_channel, _queueName);
        }

        /// <summary>
        /// implements IDisposable
        /// </summary>
        void IDisposable.Dispose()
        {
            _connection.Close();
        }

        private (IConnection connection, IModel channel)
            CreateChannel(string aHostName, string aQueueName)
        {
            var factory = new ConnectionFactory() { HostName = aHostName };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: aQueueName, durable: false,
            exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);

            return (connection, channel);
        }

        private EventingBasicConsumer CreateConsumer(IModel aChannel, string aQueueName)
        {
            var consumer = new EventingBasicConsumer(aChannel);
            aChannel.BasicConsume(queue: aQueueName,
              autoAck: false, consumer: consumer);

            consumer.Received += (model, ea) =>
            {
                string response = null;

                var body = ea.Body;
                var props = ea.BasicProperties;
                var replyProps = aChannel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    response = HandlerReceivedJson(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(" [.] " + e.Message);
                    response = "";
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    aChannel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                      basicProperties: replyProps, body: responseBytes);
                    aChannel.BasicAck(deliveryTag: ea.DeliveryTag,
                      multiple: false);
                }
            };

            return consumer;
        }
    }
}
