using System;
using System.Diagnostics;
using RabbitMQ.Client;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    public static class GlobalRabbitMqServerFunctions
    {
        /// <summary>
        ///     Provides threadsafe closure of the server.
        /// </summary>
        public static void DisableServerUnexpectedly(int index)
        {
            FactoryConnection[index].Close();
        }

        /// <summary>
        ///     Setup connection to RabbitMQ server
        /// </summary>
        /// <returns>Connection Factory</returns>
        public static ConnectionFactory SetupFactory(int index)
        {
            var factory = new ConnectionFactory
            {
                HostName = ServerInformation[index].ServerAddress.ToString(),
                UserName = ServerInformation[index].UserName,
                Password = ServerInformation[index].Password,
                VirtualHost = ServerInformation[index].VirtualHost,
                Port = ServerInformation[index].ServerPort,
                AutomaticRecoveryEnabled = ServerInformation[index].AutoRecovery,
                RequestedHeartbeat = (ushort)ServerInformation[index].ServerHeartbeat,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(ServerInformation[index].NetworkRecoveryInterval),
                TopologyRecoveryEnabled = ServerInformation[index].AutoRecovery
            };
            return factory;
        }

        /// <summary>
        ///     Creates Channel to allow queue to be formed for messaging
        /// </summary>
        /// <param name="queueDurable">Channel Deleted or not when system shut down</param>
        /// <param name="queueAutoDelete">Channel Deleted if no broker is connected</param>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        /// 
        public static void CreateQueue(bool queueDurable, bool queueAutoDelete, int index)
        {
            try
            {
                FactoryChannel[index].QueueDeclare(ServerInformation[index].ChannelName, queueDurable, false, queueAutoDelete, null);
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        ///     Creates Exchange in RabbitMQ for tying channel to to allow successful message delivery
        /// </summary>
        /// <param name="exchangeType">Manner in which Exchange behaves (Direct/Fanout/Headers/Topic)</param>
        /// <param name="exchangeDurability">Exchange Deleted if system shuts down</param>
        /// <param name="autoDelete">Exchange Deleted if no broker is connected</param>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        /// 
        public static void CreateExchange(string exchangeType, bool exchangeDurability, bool autoDelete,
            int index)
        {
            try
            {
                switch (exchangeType)
                {
                    case "direct":
                    case "Direct":
                        goto default;
                    case "fanout":
                    case "Fanout":
                        FactoryChannel[index].ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Fanout, exchangeDurability, autoDelete,
                            null);
                        break;

                    case "headers":
                    case "Headers":
                        FactoryChannel[index].ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Headers, exchangeDurability, autoDelete,
                            null);
                        break;

                    case "topic":
                    case "Topic":
                        FactoryChannel[index].ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Topic, exchangeDurability, autoDelete,
                            null);
                        break;

                    default:
                        FactoryChannel[index].ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Direct, exchangeDurability, autoDelete,
                            null);
                        break;
                }
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
                
        }

        /// <summary>
        ///     Binds predefined channel and exchange
        /// </summary>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        public static void QueueBind(int index)
        {
            try
            {
                FactoryChannel[index].QueueBind(ServerInformation[index].ChannelName, ServerInformation[index].ExchangeName, "");
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }


    }
}