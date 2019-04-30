using System;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace API.V1.RPC
{
    //
    // Summary:
    //     Provides a Request Part of Request-Reply Pattern
    //
    // Example:
    // var jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(obj);// http://www.newtonsoft.com/json/help/html/SerializingJSON.htm
    // using (var aReguest = new RpcClient())
    // {
    //   var aResJson = aReguest.Call(jsonMessage);
    //   // do something with aResJson
    //   ..
    // }
    public class RpcClient:IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer _consumer;
        private readonly IBasicProperties _props;
        private readonly BlockingCollection<string> _respQueue = new BlockingCollection<string>();


        private readonly string _hostName;
        private readonly string _queueName;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="aHostName">the host name</param>
        /// <param name="aQueueName">the queue name</param>
        public RpcClient(string aHostName = "localhost", string aQueueName = "rpc_queue")
        {
            _hostName = aHostName;
            _queueName = aQueueName;

            var channelInstanceRes = CreateChannel(_hostName);
            _connection = channelInstanceRes.connection;
            _channel = channelInstanceRes.channel;
            _replyQueueName = channelInstanceRes.replyQueueName;

            var consumerInstanceRes = CreateConsumer(_channel, _replyQueueName, _respQueue);
            _consumer = consumerInstanceRes.consumer;
            _props = consumerInstanceRes.props;
        }
        /// <summary>
        /// implements IDisposable
        /// </summary>
        void IDisposable.Dispose()
        {
            _connection.Close();
        }

        private (IConnection connection, IModel channel, string replyQueueName) 
            CreateChannel(string aHostName)
        {
            var factory = new ConnectionFactory() { HostName = aHostName };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var replyQueueName = channel.QueueDeclare().QueueName;

            return (connection, channel, replyQueueName);
        }



        private (EventingBasicConsumer consumer, IBasicProperties props) 
            CreateConsumer(IModel aChannel, string aReplyQueueName, BlockingCollection<string> aRespQueue)
        {
            var consumer = new EventingBasicConsumer(aChannel);

            var props = aChannel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = aReplyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    aRespQueue.Add(response);
                }
            };

            return (consumer, props);
        }

        /// <summary>
        /// Reguest queue to process message
        /// </summary>
        /// <param name="message">JSON</param>
        /// <returns>JSON</returns>
        public string Call(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(
                exchange: "",
                routingKey: _queueName,
                basicProperties: _props,
                body: messageBytes);

            _channel.BasicConsume(
                consumer: _consumer,
                queue: _replyQueueName,
                autoAck: true);

            return _respQueue.Take(); ;
        }
    }

}
