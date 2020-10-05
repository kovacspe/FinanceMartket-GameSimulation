using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace BurzaCashBox
{
    class AliasDictionary
    {
        private Dictionary<string, string> dict;
        public AliasDictionary()
        {
            dict = new Dictionary<string, string>();
        }

        public string[] TranslateAll(string[] sentence)
        {
            string[] translated = new string[sentence.Length];
            for(int i=0; i<sentence.Length;i++)
            {
                translated[i] = Translate(sentence[i]);
            }
            return translated;
        }

        public string Translate(string word)
        {
            string newWord;
            if (dict.TryGetValue(word, out newWord))
            {
                return newWord;
            }
            else return word;
        }

        public void WriteDict(TextWriter w)
        {
            foreach(KeyValuePair<string,string> s in dict)
            {
                w.WriteLine("{1} = {0}", s.Key, s.Value);
            }
        }

        public void Load(string fileName)
        {
            int counter = 0;
            dict = new Dictionary<string, string>();
            try
            {
                StreamReader sr = new StreamReader(fileName);
                while (!sr.EndOfStream)
                {
                    string original = sr.ReadLine();
                    string alias = sr.ReadLine();
                    while (alias != "*****")
                    {
                        dict.Add(alias, original);
                        alias = sr.ReadLine();
                        counter++;
                    }
                    
                }
                Console.WriteLine("{0} translations loaded", counter);
            }catch(IOException)
            {
                Console.WriteLine("Cannot read file");
            }
        }
    }



    class CashBoxClient
    {
        TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        AliasDictionary aliases = new AliasDictionary();
        public string Name { get; private set; }
        public bool IsActive { get; set; }
        public CashBoxClient(TcpClient client,string name )
        {
            Name = name;
            
            this.client = client;
            IsActive = true;

        }

        public bool Connect(string ipAddress)
        {
            bool connected = false;
            try
            {
                client.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), 14234));
                
                writer = new StreamWriter(client.GetStream());
                reader = new StreamReader(client.GetStream());
                Console.WriteLine("Cashbox {0} is connected to server", Name);
                connected = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Connection failed. Reason: {0}",ex.Message);
            }
            return connected;
        }

        public bool IsConnected()
        {
            return client.Connected;
        }

        public void ProcessCommand(string command)
        {
            string[] parsed = command.Split(' ');
            string[] parsedCommand = aliases.TranslateAll(parsed);
            if (parsed.Length==0)
            {
                return;
            }
            switch (parsed[0])
            {
                case "b": BuyHandler(parsedCommand); break;
                case "ld": if (parsedCommand.Length == 2) { aliases = new AliasDictionary(); aliases.Load(parsedCommand[1]); } else Console.WriteLine("Wrong file"); break;
                case "s": SellHandler(parsedCommand); break;
                case "i": InvestorInfoHandler(parsedCommand); break;
                case "settarget": SetTargetHandler(); break;
                case "help": Help();  break;
              //  case "shift": ShiftHandler(parsedCommand); break;
                case "sdic": aliases.WriteDict(Console.Out); break;
                case "price": GetPriceHandler(parsedCommand); break;
                case "end": IsActive = false; writer.WriteLine("End"); writer.Flush(); break;
                default: Console.WriteLine("Unknown command \"{0}\" ",parsedCommand[0]);  break;
            }
        }

        private void GetPriceHandler(string[] parsedCommand)
        {
            Console.WriteLine("Pozri sa na displej s burzou");
        }

        private void Help()
        {
            Console.WriteLine("Nákup komodity");
            Console.WriteLine("b NAZOV_DRUZINKY NAZOV_KOMODITY MNOZSTVO");
            Console.WriteLine("Predaj komodity");
            Console.WriteLine("s NAZOV_DRUZINKY NAZOV_KOMODITY MNOZSTVO");
            Console.WriteLine("Zobrazenie portfolia druzinky");
            Console.WriteLine("i NAZOV_DRUZINKY");
            Console.WriteLine("Nastavenie nových cielov pre komodity- Zadávajte číslo od 0 po 10");
            Console.WriteLine("settarget");
            Console.WriteLine("Zistenie ceny komodity");
            Console.WriteLine("price");
            Console.WriteLine("Zobrazenie slovnika");
            Console.WriteLine("sdic");
            Console.WriteLine("Nacita skratky zo suboru");
            Console.WriteLine("ld NAZOV_SUBORU");
            Console.WriteLine("Ukoncenie");
            Console.WriteLine("end");


        }

        private void BuyHandler(string[] parsedCommand)
        {
            double d;
            if (parsedCommand.Length != 4 || !double.TryParse(parsedCommand[3], out d))
            {
                Console.WriteLine("Wrong number of parameters.");
                return;
            }

            writer.WriteLine("BuyVerification");
            writer.WriteLine(parsedCommand[1]); // investor
            writer.WriteLine(parsedCommand[2]); // comodity
            writer.WriteLine(parsedCommand[3]); // amount
            writer.Flush();

            string answer = reader.ReadLine();
            while (answer != "TransactionVerification")
            {
                answer = reader.ReadLine();
            }
            answer = reader.ReadLine();
            if (answer == "OK")
            {
                answer = reader.ReadLine();
                Console.WriteLine("Price is {0} EUR for {1} units of {2}. Do you want to continue?(T/F)",answer,parsedCommand[3],parsedCommand[2]);
                string cont = Console.ReadLine();
                if (cont == "t" || cont =="T" || cont =="a" || cont =="A" || cont=="y" || cont == "Y")
                {

                } else
                {
                    Console.WriteLine("Transaction was cannceled.");
                    return;
                }
            }
            else
            {
                Console.WriteLine(answer);
                return;
            }

            // buy
            writer.WriteLine("Buy");
            writer.WriteLine(parsedCommand[1]); // investor
            writer.WriteLine(parsedCommand[2]); // comodity
            writer.WriteLine(parsedCommand[3]); // amount
            writer.Flush();
            while (answer != "TransactionResult")
            {
                answer = reader.ReadLine();
            }
            answer = reader.ReadLine();

            Console.WriteLine(answer);

            

        }

        private void SellHandler(string[] parsedCommand)
        {
            double d;
            if (parsedCommand.Length != 4 || !double.TryParse(parsedCommand[3], out d))
            {
                Console.WriteLine("Wrong number of parameters.");
                return;
            }
            writer.WriteLine("Sell");
            writer.WriteLine(parsedCommand[1]); // investor
            writer.WriteLine(parsedCommand[2]); // comodity
            
            writer.WriteLine(parsedCommand[3]); // amount
            writer.Flush();
            string answer = reader.ReadLine();
            while (answer != "TransactionResult")
            {
                answer = reader.ReadLine();
            }
            answer = reader.ReadLine();
            
            Console.WriteLine(answer);
            if (answer == "OK")
            {
                answer = reader.ReadLine();
                Console.WriteLine("Price: {0}",answer);
            }
        }

        private void InvestorInfoHandler(string[] parsedCommand)
        {
            if (parsedCommand.Length!=2)
            {
                Console.WriteLine("Wrong number of arguments");
                return;
            }
            writer.WriteLine("GetPortfolio");
            writer.WriteLine(parsedCommand[1]);
            writer.Flush();

            string line = reader.ReadLine();
            while (line != "InvestorPortfolio")
                line = reader.ReadLine();

            line = reader.ReadLine();
            Console.WriteLine("Portfolio of {0}", parsedCommand[1]);
            while(line!="*****")
            {
                Console.WriteLine(line);
                line = reader.ReadLine();
            }
        }

        private void SetTargetHandler()
        {
            writer.WriteLine("GetComodityNames");
            writer.Flush();

            string line = reader.ReadLine();
            while (line != "ComodityNames")
                line = reader.ReadLine();
            line = reader.ReadLine();
            Console.WriteLine("Enter new targets for comodities:");
            writer.WriteLine("SetNewTargets");
            while (line != "*****")
            {
                Console.WriteLine(line);
                Console.Write(">");
                string newTarget = Console.ReadLine();
                writer.WriteLine(line);
                writer.WriteLine(newTarget);
                line = reader.ReadLine();

            }
            writer.WriteLine("*****");
            writer.Flush();
        }
    }

    class Program
    {
        

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome! Please enter your name:");
            string name = Console.ReadLine();
           
            TcpClient client = new TcpClient();            
            CashBoxClient cb = new CashBoxClient(client,name);
            //connect
            while (!cb.IsConnected())
            {
                Console.WriteLine("Enter server IP address:");
                string ip = Console.ReadLine();
                Console.WriteLine("Connecting to server...");
                cb.Connect(ip);

            }
            Console.WriteLine();
            Console.Write(">");
            // commands
            try
            {
                while (cb.IsActive)
                {
                    string command = Console.ReadLine();
                    cb.ProcessCommand(command);
                    Console.WriteLine();
                    Console.Write(">");
                }
            } catch (IOException)
            {
                Console.WriteLine("Connection lost. Please restart cashbox!");
                Console.ReadLine();
            }
      
            
        }


    }
}
