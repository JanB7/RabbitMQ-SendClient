using static RabbitMQ_SendClient.MainWindow;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Net;
    using System.Windows.Forms;

    public static class GlobalRabbitMqServerFunctions
    {
        #region Variables & Structures

        private static readonly string DatabaseLoc =
            AppDomain.CurrentDomain.BaseDirectory + "Database\\MessageData.mdf";

        private static readonly string _connString =
            $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"{DatabaseLoc}\";Integrated Security=True";

        private static readonly StackTrace StackTracing = new StackTrace();

        #endregion Variables & Structures

        /// <summary>
        /// Provides threadsafe closure of the server. 
        /// </summary>
        public static void DisableServerUnexpectedly(int index)
        {
            FactoryConnection[index].Close();
        }

        /// <summary>
        /// Setup connection to RabbitMQ server 
        /// </summary>
        /// <returns>
        /// Connection Factory 
        /// </returns>
        private static ConnectionFactory SetupConnectionFactory(int index)
        {
            var factory = new ConnectionFactory
            {
                HostName = ServerInformation[index].ServerAddress.ToString(),
                UserName = ServerInformation[index].UserName,
                Password = ServerInformation[index].Password,
                VirtualHost = ServerInformation[index].VirtualHost,
                Port = ServerInformation[index].ServerPort,
                AutomaticRecoveryEnabled = ServerInformation[index].AutoRecovery,
                RequestedHeartbeat = (ushort) ServerInformation[index].ServerHeartbeat,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(ServerInformation[index].NetworkRecoveryInterval),
                TopologyRecoveryEnabled = ServerInformation[index].AutoRecovery
            };
            return factory;
        }

        /// <summary>
        /// Creates Channel to allow queue to be formed for messaging 
        /// </summary>
        /// <param name="queueDurable">
        /// Channel Deleted or not when system shut down 
        /// </param>
        /// <param name="queueAutoDelete">
        /// Channel Deleted if no broker is connected 
        /// </param>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        private static void CreateQueue(bool queueDurable, bool queueAutoDelete, int index)
        {
            try
            {
                FactoryChannel[index]
                    .QueueDeclare(ServerInformation[index].ChannelName, queueDurable, false,
                        queueAutoDelete, null);
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Creates Exchange in RabbitMQ for tying channel to to allow successful message delivery 
        /// </summary>
        /// <param name="exchangeType">
        /// Manner in which Exchange behaves (Direct/Fanout/Headers/Topic) 
        /// </param>
        /// <param name="exchangeDurability">
        /// Exchange Deleted if system shuts down 
        /// </param>
        /// <param name="autoDelete">
        /// Exchange Deleted if no broker is connected 
        /// </param>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        private static void CreateExchange(string exchangeType, bool exchangeDurability, bool autoDelete,
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
                        FactoryChannel[index]
                            .ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Fanout,
                                exchangeDurability, autoDelete,
                                null);
                        break;

                    case "headers":
                    case "Headers":
                        FactoryChannel[index]
                            .ExchangeDeclare(ServerInformation[index].ExchangeName,
                                ExchangeType.Headers, exchangeDurability, autoDelete,
                                null);
                        break;

                    case "topic":
                    case "Topic":
                        FactoryChannel[index]
                            .ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Topic,
                                exchangeDurability, autoDelete,
                                null);
                        break;

                    default:
                        FactoryChannel[index]
                            .ExchangeDeclare(ServerInformation[index].ExchangeName, ExchangeType.Direct,
                                exchangeDurability, autoDelete,
                                null);
                        break;
                }
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Binds predefined channel and exchange 
        /// </summary>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        private static void QueueBind(int index)
        {
            try
            {
                FactoryChannel[index]
                    .QueueBind(ServerInformation[index].ChannelName,
                        ServerInformation[index].ExchangeName, "");
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// TODO Save Settings to File 
        /// </summary>
        /// <param name="uidGuid">
        /// </param>
        private static void SetupFactory(Guid uidGuid)
        {
            var index = GetIndex<CheckListItem>(uidGuid);
            if (index == -1) return;
            Array.Resize(ref ServerInformation, ServerInformation.Length + 1);
            ServerInformation[ServerInformation.Length - 1] = SetDefaultSettings(uidGuid);

            var factoryChannel = FactoryChannel;
            if (factoryChannel != null)
            {
                Array.Resize(ref factoryChannel, factoryChannel.Length + 1);
                factoryChannel = new IModel[factoryChannel.Length];
                FactoryChannel = factoryChannel;
            }

            var factoryConnection = FactoryConnection;
            if (factoryConnection != null)
            {
                Array.Resize(ref factoryConnection, factoryConnection.Length + 1);
                factoryConnection = new IConnection[factoryConnection.Length];
                FactoryConnection = factoryConnection;
            }

            var factory = Factory;
            if (factory != null)
            {
                Array.Resize(ref factory, factory.Length + 1);
                factory = new IConnectionFactory[factory.Length];
                Factory = factory;
            }
        }

        public static void ShutdownFactory(Guid uidGuid)
        {
            var index = GetIndex<RabbitServerInformation>(uidGuid);

            RemoveAtIndex<RabbitServerInformation>(index, ServerInformation);

            if (FactoryChannel[index].IsOpen) FactoryChannel[index].Close();
            RemoveAtIndex<IModel>(index, FactoryChannel);

            if (FactoryConnection[index].IsOpen) FactoryConnection[index].Close();
            RemoveAtIndex<IConnection>(index, FactoryConnection);

            RemoveAtIndex<IConnectionFactory>(index, Factory);
        }

        public static void StartServer()
        {
            try
            {
                Factory[Factory.Length - 1] = new ConnectionFactory();
                Factory[Factory.Length - 1] = SetupConnectionFactory(Factory.Length - 1);
                FactoryConnection[Factory.Length - 1] = Factory[Factory.Length - 1].CreateConnection();

                FactoryChannel[Factory.Length - 1] = FactoryConnection[Factory.Length - 1].CreateModel();

                FactoryConnection[Factory.Length - 1].AutoClose = false;

                CreateQueue(true, false, Factory.Length - 1);
                CreateExchange(ExchangeType.Direct, true, false, Factory.Length - 1);
                QueueBind(Factory.Length - 1);
            }
            catch (BrokerUnreachableException ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                var message = ErrorType[1003];
                const string caption = "Broker Unreachable Exception";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                var message = ex.Message;
                var caption = "Error in: " + ex.Source;
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static RabbitServerInformation SetDefaultSettings(Guid uidGuid)
        {
            var rabbitInfo = new RabbitServerInformation
            {
                UidGuid = uidGuid,
                AutoRecovery = true,
                ServerAddress = IPAddress.Parse("130.113.130.194"),
                ExchangeName = "Default",
                ChannelName = "Default",
                UserName = "User",
                Password = "Factory1",
                VirtualHost = "default",
                ServerPort = 5672,
                ServerHeartbeat = 30,
                Encoding = "UTF8",
                MessageType = "Serial",
                MessageFormat = "jsonObject",
                NetworkRecoveryInterval = 5
            };
            return rabbitInfo;
        }

        public static bool? UserConfigureFactory(Guid uidGuid)
        {
            SetupFactory(uidGuid);
            ServerInformation[ServerInformation.Length - 1] = SetDefaultSettings(uidGuid);

            var configureServer = new SetupServer(uidGuid);
            return configureServer.ShowDialog();
        }

        internal static void ProtectData(Guid uidGuid, string message, string deviceName)
        {
            var index = GetIndex<RabbitServerInformation>(uidGuid);
            if (index == -1) return;
            else index--;
            

            var datInfo = new DataBaseInfo
            {
                Message = message,
                TimeStamp = DateTime.Now,
                FriendlyName = FriendlyName[index],
                Channel = ServerInformation[index].ChannelName,
                Exchange = ServerInformation[index].ExchangeName,
                ServerAddress = ServerInformation[index].ServerAddress.ToString(),
                DeliveryTag = Guid.NewGuid(),
                DeviceType = deviceName
            };

            const string sqlString =
                "INSERT [dbo].[MessageData] (Message,TimeStamp,FriendlyName,Channel,Exchange,ServerAddress,DeliveryTag,DeviceType)VALUES"
                +
                "(@message,@timestamp,@friendlyname,@channel,@exchange,@serveraddress,@deliverytag,@devicetype)";

            using (var conn = new SqlConnection(_connString))
            {
                var command = new SqlCommand(sqlString, conn)
                {
                    CommandType = CommandType.Text
                };
                command.Parameters.AddWithValue("@message", datInfo.Message);
                command.Parameters.AddWithValue("@timestamp", datInfo.TimeStamp);
                command.Parameters.AddWithValue("@friendlyname", datInfo.FriendlyName);
                command.Parameters.AddWithValue("@channel", datInfo.Channel);
                command.Parameters.AddWithValue("@exchange", datInfo.Exchange);
                command.Parameters.AddWithValue("@serveraddress", datInfo.ServerAddress);
                command.Parameters.AddWithValue("@deliverytag", datInfo.DeliveryTag);
                command.Parameters.AddWithValue("@devicetype", datInfo.DeviceType);
                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        internal static void RemoveProtectedData(Guid uidGuid)
        {
            const string sqlString = "DELETE FROM[dbo].[MessageData] WHERE [DeliveryTag] = @uuid";

            using (var conn = new SqlConnection(_connString))
            {
                try
                {
                    var command = new SqlCommand(sqlString, conn) {CommandType = CommandType.Text};
                    command.Parameters.AddWithValue("@uuid", uidGuid);
                    conn.Open();
                    command.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    if (conn.State == ConnectionState.Open)
                        conn.Close();
                    var sf = StackTracing.GetFrame(0);
                    LogError(ex, LogLevel.Critical, sf);
                }
            }
        }
    }

    internal struct DataBaseInfo
    {
        internal string Message { get; set; }
        internal DateTime TimeStamp { get; set; }
        internal string FriendlyName { get; set; }
        internal string Channel { get; set; }
        internal string Exchange { get; set; }
        internal string ServerAddress { get; set; }
        internal Guid DeliveryTag { get; set; }
        internal string DeviceType { get; set; }
    }
}