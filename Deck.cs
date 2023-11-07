using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
	public class Deck
	{
		private ulong[] cards;
		private ulong removedCards;
		private int position;
		private Random random;

		// TODO: this metric doesn't account for removed cards
		public int CardsRemaining { get { return 52 - position; } }

		public Deck(ulong removedCards = 0)
		{
			this.removedCards = removedCards;
			random = new Random();
			cards = new ulong[52];

			// Generate cards as uInt64 1, 10, 100, ...
			for (int i = 0; i < 52; i++) cards[i] = 1ul << i;

			// There are no card objects in the deck, instead card draw is simulated by "position", an int used as index for the "cards" ulong[]
			position = 0;
		}

		public void Shuffle()
		{
			int n = cards.Length;
			while (n > 1)
			{
				n--;
				int k = random.Next(n + 1);

				// Swap card values at index n and k
				ulong value = cards[k];
				cards[k] = cards[n];
				cards[n] = value;
			}

			// Reset position to top card, thus shuffle "refills" the deck
			position = 0;
		}

		// TODO: Draw actual cards
		public ulong Draw(int count)
		{
			ulong hand = 0;
			for (int i = 0; i < count; i++)
			{
				// Skip cards that were manually removed via removedCards
				while ((cards[position] & removedCards) != 0) position++;

				// Add cards to bitmap hand through logical OR (result is 1 if either bit is 1) compound assignment
				hand |= cards[position];
				position++;
			}
			return hand;
		}
	}
}
