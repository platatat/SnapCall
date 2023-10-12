using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
	public class HandStrength : IComparable<HandStrength>
	{
		public HandRanking HandRanking { get; set; }
		// Kickers are processed as underlying enum type of Rank from Card. See Hand.GetStrength 
		public List<int> Kickers { get; set; }

		// Standard CompareTo implementation. See CompareTo summary (mouseover)
		public int CompareTo(HandStrength other)
		{
			if (this.HandRanking > other.HandRanking) return 1;
			else if (this.HandRanking < other.HandRanking) return -1;

			for (var i = 0; i < this.Kickers.Count; i++)
			{
				if (this.Kickers[i] > other.Kickers[i]) return 1;
				if (this.Kickers[i] < other.Kickers[i]) return -1;
			}

			return 0;
		}
	}
}
