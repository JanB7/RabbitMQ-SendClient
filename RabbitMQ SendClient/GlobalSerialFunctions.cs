using System;
using System.Diagnostics;
using System.Windows.Forms;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    public static class GlobalSerialFunctions
    {
        private static readonly int[] StatsGroupings = new int[SerialPorts.Length];
        private static readonly StackTrace StackTracing = new StackTrace();

        /// <summary>
        ///     Calculates Upper Control limit for the messages
        /// </summary>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        public static void CalculateNpChart(int index)
        {
            var pVal = SerialCommunications[index].InformationErrors / SerialCommunications[index].TotalInformationReceived;
            var xBar = 0.0;
            if (SerialCommunications[index].TotalInformationReceived > SerialCommunications[index].MaximumErrors)
            {
                if (StatsGroupings[index] > 48)
                    StatsGroupings[index] = 0;

                StatsGroupings[index]++;
            }
            SerialCommunications[index].SetX(StatsGroupings[index], pVal);
            for (var i = 0; i < 50; i++)
                xBar += SerialCommunications[index].GetX(i);

            xBar = xBar / 50;

            SerialCommunications[index].UCL = xBar + 3 *
                                              Math.Sqrt(Math.Abs(xBar * (1 - xBar)) /
                                                        SerialCommunications[index].MaximumErrors);
        }

        /// <summary>
        ///     Tests if message errors are out of control
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool OutOfControl(int index)
        {
            try
            {
                var xbar = 0.0;
                for (var i = SerialCommunications[index].MaximumErrors; i >= 0; i--)
                    xbar += SerialCommunications[index].GetX(i);
                xbar = xbar / (SerialCommunications[index].MaximumErrors * 0.9);

                return xbar > SerialCommunications[index].UCL;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }
    }
}