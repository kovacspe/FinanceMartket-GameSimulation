using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burza
{
    public class MarketException: Exception {
        public string ExceptionDetail;
        public MarketException(string msg)
        {
            ExceptionDetail = msg;
        }
    }

    static class NormalDistribution
    {
        static Random generator = new Random(); 
        public static double GetRandomFromNormalDistribution(double expectedValue, double standardDeviation)
        {
            
            double u1 = 1.0 - generator.NextDouble(); 
            double u2 = 1.0 - generator.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                         expectedValue + standardDeviation * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        } 
    }


    public interface IComodity
    {
        string Name { get; }
        System.Drawing.Pen Pen {get;}
        void Recalculate(MarketModel model);
        List<double> History { get;}
        double CurrentValue { get; }
        void SetNewTarget(double target);
        double BuyComodity(double amount);
        double SellComodity(double amount);
        void ShiftComodityValue(double price);
        void ToStream(StreamWriter sw);
        IList<double> GetNormalisedHistory(MarketModel model);


    }

    public struct ComodityItem
    {
        public ComodityItem(double amount, IComodity comodity)
        {
            Amount = amount;
            Comodity = comodity;
        }

        public void ToStream(StreamWriter sw)
        {
            sw.WriteLine(Comodity.Name);
            sw.WriteLine(Amount);
        }

        public IComodity Comodity;
        public double Amount;
    }

    public class Company : IComodity
    {
        private double BUYSELLCOEF = 0.2;
        private double CHANGECOEF = 0.05;
        private double TargetValue;

        public System.Drawing.Pen Pen { get; private set; }
        public double CurrentValue { get; private set; }
        public List<double> History { get; private set; }
        public string Name { get; private set; }
        private double OnMarket;

        public Company(string name, double target, double current, double onMarket, string color)
        {
            Name = name;
            TargetValue = target;
            CurrentValue = current;
            OnMarket = onMarket;
            History = new List<double>();
            Pen =  new System.Drawing.Pen(System.Drawing.Color.FromName(color));
        }


        public void SetNewTarget(double target)
        {
            TargetValue = Math.Max(TargetValue + (target - 5) * 10,1);
        }

        public double BuyComodity(double amount)
        {
            if (amount>OnMarket)
            {
                throw new MarketException("Not enough stocks on the market");
            }
            else
            {
                OnMarket -= amount;
                double lastValue = CurrentValue;
                TargetValue = TargetValue + (amount * BUYSELLCOEF);
                return lastValue * amount;
            }
        }

        public IList<double> GetNormalisedHistory(MarketModel model)
        {

            IList<double> h = new List<double>();
            double max = History[History.Count - 1];

            for(int i=Math.Max(History.Count-100,0);i<History.Count;i++)
            {
                h.Add(0.05+(History[i] / model.BestCompanyValue)*0.9);
            }
            return h;
        }

        public void Recalculate(MarketModel model)
        {
            //if (History.Count > 100) History.RemoveAt(0);
            if (model.BestCompanyValue < CurrentValue) model.BestCompanyValue = CurrentValue;
            CurrentValue = CurrentValue+ Math.Max(-1, Math.Min(CHANGECOEF * (TargetValue - CurrentValue),1));
            History.Add(CurrentValue);
        }

        public double SellComodity(double amount)
        {
            double lastValue = CurrentValue;
            OnMarket += amount; 
            TargetValue = TargetValue - (amount * BUYSELLCOEF);
            return lastValue * amount;
        }

        public void ShiftComodityValue(double price)
        {
            TargetValue += price;
        }

        public void ToStream(StreamWriter sw)
        {
            sw.WriteLine("COMPANY");
            sw.WriteLine(Name);
            sw.WriteLine(TargetValue);
            sw.WriteLine(CurrentValue);
            sw.WriteLine(OnMarket);
            sw.WriteLine(Pen.Color.ToString());
        }
    }

    public class Investor
    {
        Dictionary<string,ComodityItem> Portfolio;
        public string Name { get; private set; }

        public Investor(string name,Dictionary<string,ComodityItem> port)
        {
            Name = name;
            Portfolio = port;
        }

        public void BuyComodity(string comodityName, double amount,IComodity comodity)
        {
            ComodityItem comItem;
            if (Portfolio.TryGetValue(comodityName, out comItem))
            {
              
                    comItem.Amount += amount;
                    comItem.Comodity.BuyComodity(amount);
                    Portfolio[comodityName] = comItem;

           
               
            }
            else
            {
                comodity.BuyComodity(amount);
                
                Portfolio.Add(comodityName, new ComodityItem(amount,comodity));
            }
        }

        public void SellComodity(string comodityName, double amount)
        {
            ComodityItem comItem;
            if (Portfolio.TryGetValue(comodityName, out comItem))
            {
                if (amount<=comItem.Amount)
                {
                    comItem.Amount -= amount;
                    comItem.Comodity.SellComodity(amount);
                    Portfolio[comodityName] = comItem;

                }
                else throw new MarketException("Not enough resources");
            }
            else throw new MarketException("Not enough resources or unknown comodity");
        }

        public List<ComodityItem> GetPortfolioAsList()
        {
            List<ComodityItem> port = new List<ComodityItem>();
            foreach(KeyValuePair<string,ComodityItem> item in Portfolio)
            {
                port.Add(item.Value);
            }
            return port;
        }

    }

    public class Comodity : IComodity
    {
        private double BUYSELLCOEF = 0.1;
        private double CHANGECOEF = 0.4;
        public System.Drawing.Pen Pen { get { return new System.Drawing.Pen(System.Drawing.Color.Red);  } }
        public string Name { get; }
        private double ExpectedValue;
        private double Variance;
        private double MinValue;
        private double MaxValue;
        public double CurrentValue { get; private set; }

        public IList<double> GetNormalisedHistory(MarketModel model)
        {
            IList<double> h = new List<double>();
            for (int i = Math.Max(0, History.Count - 100); i < History.Count; i++)
            {
                h.Add(0.05 + 0.9*(History[i]-MinValue)/(MaxValue-MinValue));
            }
            return h;
        }

        public Comodity(string name, double expectedValue, double varinace, double min, double max, double current)            
        {
            Name = name;
            ExpectedValue = expectedValue;
            //Variance = Math.Sqrt((max-min)/2);
            Variance = varinace;
            MinValue = min;
            MaxValue = max;
            CurrentValue = current;
            History = new List<double>();
        }

        public List<double> History { get; private set; }

        public double BuyComodity(double amount)
        {
            double price = amount * CurrentValue;
            CurrentValue += amount * BUYSELLCOEF;
            if (CurrentValue > MaxValue) CurrentValue = MaxValue;
            return price;
        }


        public void Recalculate(MarketModel model)
        {
            
            CurrentValue += (NormalDistribution.GetRandomFromNormalDistribution(ExpectedValue, Variance)-CurrentValue)*CHANGECOEF;
            History.Add(CurrentValue);

            if (CurrentValue > MaxValue) CurrentValue = MaxValue;
            if (CurrentValue < MinValue) CurrentValue = MinValue;
        }

        public double SellComodity(double amount)
        {
            double price = amount * CurrentValue;
            CurrentValue -= amount * BUYSELLCOEF;
            if (CurrentValue < MinValue) CurrentValue = MinValue;
            return price;
        }

        public void ShiftComodityValue(double price)
        {
            CurrentValue += price;
            if (CurrentValue > MaxValue) CurrentValue = MaxValue;
            if (CurrentValue < MinValue) CurrentValue = MinValue;
        }

        public void SetNewTarget(double target)
        {
            this.ExpectedValue =MinValue +  target * (MaxValue - MinValue) / 10;
        }

        public void ToStream(StreamWriter sw)
        {
            sw.WriteLine("COMODITY");
            sw.WriteLine(Name);
            sw.WriteLine(ExpectedValue);
            sw.WriteLine(Variance);
            sw.WriteLine(MinValue);
            sw.WriteLine(MaxValue);
            sw.WriteLine(CurrentValue);
        }
    }
}
