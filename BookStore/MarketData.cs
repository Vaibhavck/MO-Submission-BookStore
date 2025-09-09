using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore
{
    internal class MarketData
    {
        public string Type { get; set; }

        public string Action { get; set; }

        public long Id { get; set; }

        public string Symbol { get; set; }

        public string Side { get; set; }

        public Decimal Price { get; set; }

        public long Quantity { get; set; }

        public DateTime SendTimestamp { get; set; }

        public long RawTimestamps { get; set; }
    }
}
