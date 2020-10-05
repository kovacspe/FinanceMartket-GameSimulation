using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Burza
{
    

    class CashBox
    {
        public readonly string MessageEnd = "*****";
        TcpClient tcpClient;
        MarketModel market;
        public CashBox(TcpClient client,MarketModel market)
        {
            tcpClient = client;
            this.market = market;
        }


        public void Listen()
        {
            NetworkStream stream = tcpClient.GetStream();
            bool end = false;
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            try
            {
                while (!reader.EndOfStream && !end)
                {
                    string command = reader.ReadLineAsync().Result;
                    lock (market.marketLock)
                    {
                        switch (command)
                        {
                            case "GetPortfolio": GetPortfolioHandler(reader, writer); break;
                            case "Buy": BuySellHandler(reader, writer, true); break;
                            case "Sell": BuySellHandler(reader, writer, false); break;
                            case "BuyVerification": BuyVerificationHandler(reader, writer); break;
                            case "End": end = true; break;
                            case "GetComodityNames": GetComodityNamesHandler(reader, writer); break;
                            case "Shift": ShiftHandler(reader, writer); break;
                            case "SetNewTargets": SetNewTargetsHandler(reader, writer); break;

                        }
                    }

                }
            }catch(IOException)
            {

            }

        }

        private void GetComodityNamesHandler(StreamReader reader, StreamWriter writer)
        {
            writer.WriteLine("ComodityNames");
            IEnumerable<string> names = market.GetComodityNames();
            foreach(string s in names)
            {
                writer.WriteLine(s);
            }
            writer.WriteLine(MessageEnd);
            writer.Flush();
        }
        private void ShiftHandler(StreamReader reader, StreamWriter writer)
        {
            try
            {
                string comodity = reader.ReadLine();
                double shift = double.Parse(reader.ReadLine());
                market.ShiftComodity(comodity, shift);
            }
            catch (Exception)
            {

            }
        }

        private void SetNewTargetsHandler(StreamReader reader, StreamWriter writer)
        {
            string line = reader.ReadLine();
            while (line != MessageEnd)
            {
                try
                {
                    string comodityName = line;
                    double newTarget = double.Parse(reader.ReadLine());
                    line = reader.ReadLine();
                    market.SetNewTarget(comodityName, newTarget);
                }catch(Exception)
                {

                }
                }
            writer.Flush();
        }

        private void BuyVerificationHandler(StreamReader reader, StreamWriter writer)
        {
            string investorName = reader.ReadLine();
            string comodityName = reader.ReadLine();
            double amount = double.Parse(reader.ReadLine());
            writer.WriteLine("TransactionVerification");
            try
            {

                double price = market.GetPrice(comodityName)*amount;
                writer.WriteLine("OK");
                writer.WriteLine(price);

            }
            catch (MarketException ex)
            {
                writer.WriteLine(ex.ExceptionDetail);
            }
            writer.Flush();
        }

        private void BuySellHandler(StreamReader reader, StreamWriter writer, bool isBuying)
        {
            string investorName = reader.ReadLine();
            string comodityName = reader.ReadLine();
            double amount = double.Parse(reader.ReadLine());
            writer.WriteLine("TransactionResult");
            try
            {
                if (isBuying)
                {
                    market.BuyComodity(investorName, comodityName, amount);
                    writer.WriteLine("OK");
                }
                else
                {
                    double price = market.GetPrice(comodityName);
                    market.SellComodity(investorName, comodityName, amount);
                    writer.WriteLine("OK");
                    writer.WriteLine(amount*price);
                }
            } catch(MarketException ex)
            {
                writer.WriteLine(ex.ExceptionDetail);
            }
            writer.Flush();


        }

        

        private void GetPortfolioHandler(StreamReader reader,StreamWriter sw)
        {
            string InvestorName = reader.ReadLine();

            List<ComodityItem> portfolio =  market.GetInvestorPortfolio(InvestorName);
            sw.WriteLine("InvestorPortfolio");
            foreach(ComodityItem item in portfolio)
            {
                item.ToStream(sw);
            }
            sw.WriteLine(MessageEnd);
            sw.Flush();
        }
    }
}
