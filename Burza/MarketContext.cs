using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Burza
{
    public class MarketModel
    {
        public object marketLock = new object();
        Dictionary<string, Investor> investors ;
        public Dictionary<string, IComodity> ComoditiesOnMarket { get; private set; }
        public double BestCompanyValue {
            get; set;
            
        }
        public MarketModel()
        {
            ComoditiesOnMarket = new Dictionary<string, IComodity>();
            investors = new Dictionary<string, Investor>();
        }

        public IEnumerable<string> GetComodityNames()
        {
            return ComoditiesOnMarket.Keys;
        }

        public void ShiftComodity(string comodityName, double shift)
        {
            IComodity com;
            if (ComoditiesOnMarket.TryGetValue(comodityName, out com))
            {
                com.ShiftComodityValue(shift);
            }
            else throw new MarketException("Unknown comodity");
        }

        public void NextRound()
        {
            foreach(KeyValuePair<string,IComodity> com in ComoditiesOnMarket)
            {
                com.Value.Recalculate(this);
            }
        }

        public void SetNewTarget(string comodityName, double target)
        {
            IComodity com;
            if (ComoditiesOnMarket.TryGetValue(comodityName, out com))
            {
              
                  com.SetNewTarget(target);
              

            }
            else throw new MarketException("Unknown comodity");
        }

        public double GetPrice(string comodityName)
        {
            IComodity com;
            if (ComoditiesOnMarket.TryGetValue(comodityName, out com))
            {
                return com.CurrentValue;
            }
            else throw new MarketException("Unknown comodity");
        }

        public List<ComodityItem> GetInvestorPortfolio(string investorName)
        {
            Investor inv;
            if (investors.TryGetValue(investorName, out inv))
            {
                lock(inv)
                {
                    return inv.GetPortfolioAsList();
                }
                

            }
            else throw new MarketException("Unknown investor");
        }

        public void LoadInvestors(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            while (!sr.EndOfStream)
            {
                string InvestorName = sr.ReadLine();
                
                string line = sr.ReadLine();
                Dictionary<string,ComodityItem> comodities=new Dictionary<string, ComodityItem>();
                while (line!="*****")
                {
                    
                    IComodity com;
                    if (ComoditiesOnMarket.TryGetValue(line, out com)) { } else throw new MarketException("Unknown comodity in input file.");
                    line = sr.ReadLine();
                    double amount = Double.Parse(line);
                    ComodityItem item = new ComodityItem(amount,com);
                    comodities.Add(item.Comodity.Name, item);
                    line = sr.ReadLine();
                }
                Investor investor = new Investor(InvestorName,comodities);
                investors.Add(InvestorName,investor);
            }
        }

        public void LoadComodities(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            while (!sr.EndOfStream)
            {
                string type = sr.ReadLine();
                if (type == "COMODITY")
                {
                    string ComodityName = sr.ReadLine();
                    double ExpectedValue = double.Parse(sr.ReadLine());
                    double Variance = double.Parse(sr.ReadLine());
                    double min = double.Parse(sr.ReadLine());
                    double max = double.Parse(sr.ReadLine());
                    double current = double.Parse(sr.ReadLine());
                    Comodity com = new Comodity(ComodityName, ExpectedValue,Variance, min, max, current);
                    ComoditiesOnMarket.Add(ComodityName, com);
                }
                else if (type == "COMPANY")
                {
                    string ComodityName = sr.ReadLine();
                    double target = double.Parse(sr.ReadLine());
                    double current = double.Parse(sr.ReadLine());
                    double onMarket = double.Parse(sr.ReadLine());
                    string penColor = sr.ReadLine();
                    Company com = new Company(ComodityName, target, current, onMarket,penColor);
                    ComoditiesOnMarket.Add(ComodityName, com);
                }
                else throw new InvalidDataException();

            }
        }
        public void BuyComodity(string investorName, string comodityName,double amount)
        {
            Investor inv;
            IComodity com;
            if (ComoditiesOnMarket.TryGetValue(comodityName, out com))
            {
                
            }
            else throw new MarketException("Unknown comodity");
            if (investors.TryGetValue(investorName, out inv))
            {
                inv.BuyComodity(comodityName, amount,com);
            }
            else throw new MarketException("Unknown investor");
        }
        public void SellComodity(string investorName, string comodityName, double amount)
        {
            Investor inv;
            if (investors.TryGetValue(investorName, out inv))
            {
                inv.SellComodity(comodityName, amount);
            }
            else throw new MarketException("Unknown investor");
        }

        public void MakeBackup()
        {
            string name =  "Backup.txt";
            StreamWriter w = new StreamWriter("comodities" + name);
            foreach (KeyValuePair<string,IComodity> c in ComoditiesOnMarket)
            {
                c.Value.ToStream(w);
            }
            w.Close();
            w = new StreamWriter("investors"+name);
            foreach (KeyValuePair<string, Investor> c in investors)
            {
                w.WriteLine(c.Value.Name);
                foreach(ComodityItem it in c.Value.GetPortfolioAsList())
                {
                    it.ToStream(w);
                }
                w.WriteLine("*****");

            }
            w.Close();
        }
    }
}
