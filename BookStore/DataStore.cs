using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore
{
    internal class DataStore
    {
        private readonly List<MarketData> _orderBook = new List<MarketData>();
        private readonly List<MarketData> _tradeBook = new List<MarketData>();
        private readonly object _lock = new object();

        public int OrderBookCount => this._orderBook.Count;

        public int TradeBookCount => this._tradeBook.Count;

        public void AddOrUpdate(MarketData data)
        {
            lock (this._lock)
            {
                List<MarketData> book = this.GetBook(data.Type);
                if (data.Action == "INSERT")
                    book.Add(data);
                else if (data.Action == "UPDATE")
                {
                    MarketData marketData = book.FirstOrDefault<MarketData>((Func<MarketData, bool>)(d => d.Id == data.Id));
                    if (marketData == null)
                        return;
                    marketData.Symbol = data.Symbol;
                    marketData.Side = data.Side;
                    marketData.Price = data.Price;
                    marketData.Quantity = data.Quantity;
                }
                else
                {
                    if (!(data.Action == "DELETE"))
                        return;
                    book.RemoveAll((Predicate<MarketData>)(d => d.Id == data.Id));
                }
            }
        }

        public List<object> GetData(string bookType, int rowIndex)
        {
            lock (this._lock)
            {
                List<MarketData> book = this.GetBook(bookType);
                if (rowIndex >= 0 && rowIndex < book.Count)
                {
                    MarketData marketData = book[rowIndex];
                    return new List<object>()
                        {
                          (object) marketData.Id,
                          (object) marketData.Symbol,
                          (object) marketData.Side,
                          (object) marketData.Price,
                          (object) marketData.Quantity,
                          (object) "Col6",
                          (object) "Col7",
                          (object) "Col8",
                          (object) "Col9",
                          (object) "Col10",
                          (object) "Col11",
                          (object) "Col12",
                          (object) "Col13",
                          (object) "Col14",
                          (object) "Col15",
                          (object) "Col16",
                          (object) "Col17",
                          (object) "Col18",
                          (object) "Col19",
                          (object) "Col20",
                          (object) "Col21",
                          (object) "Col22",
                          (object) "Col23",
                          (object) "Col24",
                          (object) "Col25",
                          (object) "Col26",
                          (object) "Col27",
                          (object) "Col28",
                          (object) "Col29",
                          (object) "Col30",
                          (object) "Col31",
                          (object) "Col32",
                          (object) "Col33",
                          (object) "Col34",
                          (object) "Col35",
                          (object) "Col36",
                          (object) "Col37",
                          (object) "Col38",
                          (object) "Col39",
                          (object) "Col40",
                          (object) "Col41",
                          (object) "Col42",
                          (object) "Col43",
                          (object) "Col44",
                          (object) "Col45",
                          (object) "Col46",
                          (object) "Col47",
                          (object) "Col48",
                          (object) "Col49",
                          (object) "Col50"
                        };
                }
            }
            return (List<object>)null;
        }

        private List<MarketData> GetBook(string type)
        {
            return type.ToLower() == "order" ? this._orderBook : this._tradeBook;
        }
    }
}
