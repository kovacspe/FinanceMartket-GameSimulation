using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Burza
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            MarketModel market = new MarketModel();
            market.LoadComodities("comodities.txt");
            market.LoadInvestors("investors.txt");
            Thread acceptingCashBoxes = new Thread(() => Listen(market));
            acceptingCashBoxes.IsBackground = true;
            acceptingCashBoxes.Start();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(market));
        }

        static void Listen(MarketModel market)
        {
            TcpListener listener = new TcpListener(System.Net.IPAddress.Any,14234);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                CashBox cashBox = new CashBox(client,market);
                Thread cb = new Thread(() => { cashBox.Listen(); });
                cb.IsBackground = true;
                cb.Start();
               // market.AddCashBox(cashBox);
            }

        }
    }
}
