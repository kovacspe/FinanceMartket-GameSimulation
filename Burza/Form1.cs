using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Burza
{

   

    public partial class Form1 : Form
    {
        private MarketModel market;
        List<ComodityComponent> coms = new List<ComodityComponent>();
        private int RefreshInterval = 30000;
        public Form1(MarketModel market)
        {
            InitializeComponent();
           
            this.market = market;
            CreateComponents();

            MarketTimer.Interval = RefreshInterval;
            MarketTimer.Enabled = true;
            Text = "Burza";
           
        }



        private void CreateComponents()
        {
            TableLayoutPanel p = new TableLayoutPanel();
          
            //p.AutoSize = true;
            p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            p.ColumnCount = 3;
            p.RowCount = market.ComoditiesOnMarket.Count/ p.ColumnCount + Math.Min(1,market.ComoditiesOnMarket.Count % p.ColumnCount);
            
            p.Resize += new EventHandler((object o, EventArgs e) =>
            {
                foreach (ComodityComponent c in coms)
                {
                    c.Panel.Width = (int)(Math.Floor(p.Width / (double) p.ColumnCount));
                    c.Panel.Height = (int)(Math.Floor(p.Height / (double) p.RowCount));
                }
            });

            this.Resize += new EventHandler((object o, EventArgs e) =>
            {
               
                    p.Width = ClientSize.Width;
                    p.Height = ClientSize.Height;
                
            });

            foreach (KeyValuePair<string,IComodity> com in market.ComoditiesOnMarket)
            {
                ComodityComponent component = new ComodityComponent(com.Value.Name);
                component.Panel.Margin = new Padding(0,0,5,0);
                p.Controls.Add(component.Panel);
                
                coms.Add(component);
                
            }

            //mix
            ComodityComponent c1 = new ComodityComponent("MIX");
            c1.Panel.Margin = new Padding(0, 0, 5, 0);
            p.Controls.Add(c1.Panel);

            coms.Add(c1);

            this.Controls.Add(p);
        }

        private void MarketTimer_Tick(object sender, EventArgs e)
        {
            market.NextRound();
            market.MakeBackup();
            RefreshScreen();
        }

        private void RefreshScreen()
        {
            foreach (ComodityComponent c in coms)
            {
                if (c.Name == "MIX")
                {
                    List<IList<double>> histories = new List<IList<double>>();
                    IList<Pen> pens = new List<Pen>();
                    foreach(IComodity com in market.ComoditiesOnMarket.Values)
                    {
                        if (com.GetType() == typeof(Company))
                        {
                            histories.Add(com.GetNormalisedHistory(market));
                            pens.Add(((Company) com).Pen);
                        }
                    }
                    c.RefreshMultiple(histories,pens);
                }
                else
                {
                    c.Refresh(market.ComoditiesOnMarket[c.Name].GetNormalisedHistory(market), market.ComoditiesOnMarket[c.Name].Pen);
                    c.SetCurrentPrice(market.ComoditiesOnMarket[c.Name].CurrentValue);
                }
            }
        }
    }

    public class ComodityComponent
    {
        public string Name;
        private int maxPriceWindowSize = 200;
        private Label lblName;
        private Label lblPrice;
        private Graphics graph;
        private Panel graphPanel;
        public FlowLayoutPanel Panel { get; private set; }
        List<double> priceWindow;

        public ComodityComponent(string name)
        {
            lblName = new Label();
            lblName.Height = 15;
           
            this.Name = name;
            lblName.Text = name;
            lblName.Margin = new Padding(0);
            //lblName.AutoSize = true;
            priceWindow = new List<double>(maxPriceWindowSize);
            Panel = new FlowLayoutPanel();
            Panel.Controls.Add(lblName);
            graphPanel = new Panel();
            graphPanel.Margin = new Padding(0);
            //graphPanel.AutoSize = true;
            Panel.Controls.Add(graphPanel);
            /*
            lblPrice = new Label();
            lblPrice.AutoSize = true;
            lblPrice.Height = 20;
            lblPrice.Width = 80;
            lblPrice.Text = "NO DATA";
            
            Panel.Controls.Add(lblPrice);
            */
            //Panel.AutoSize=true;
            Panel.Resize += new EventHandler ((object sender, EventArgs e) =>
                  {
                      lblName.Width = Panel.Width;
                      graphPanel.Width = Panel.Width;
                      lblName.Height = 15;
                      graphPanel.Height = Math.Max(10, Panel.Height - 15);
                  }
                );
        }

        public void Show()
        {

        }

        public void SetCurrentPrice(double price)
        {
            lblName.Text = Name + " " + price.ToString("N3");
        }

        public void RefreshMultiple(IList<IList<double>> multihist,IList<Pen> col)
        {
            if (multihist[0].Count < 2) return;

            graph = graphPanel.CreateGraphics();
            graph.Clear(Color.White);

            
            for (int i = 0; i < multihist.Count; i++)
            { 
                PointF[] points = new PointF[multihist[i].Count];
                for (int j = 0; j < multihist[i].Count; j++)
                {
                    points[j].X = j * graphPanel.Width / multihist[i].Count;
                    points[j].Y = 0.01f + (float)(1 - multihist[i][j]) * (graphPanel.Height - 0.02f);
                }
                graph.DrawCurve(col[i], points);
            }
            
        }

        public void Refresh(IList<double> history,Pen pen)
        {
            //lblPrice.Text = history[history.Count - 1].ToString();
            //lblName.Text = Name + " "+  history[history.Count - 1].ToString("N3");
            if (history.Count < 2) return;
            
            graph = graphPanel.CreateGraphics();
            graph.Clear(Color.White);
            
            PointF[] points = new PointF[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                points[i].X = i * graphPanel.Width/history.Count;
                points[i].Y =0.01f + (float) (1-history[i])*(graphPanel.Height-0.02f);
            }
            graph.DrawCurve(pen, points);
            

        }

        public void AddNextPrice(double price)
        {
            lblPrice.Text = price.ToString();
            priceWindow.Add(price);
            if (priceWindow.Count > maxPriceWindowSize)
            {
                priceWindow.RemoveAt(0);
            }
        }
    }
}
