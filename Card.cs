using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
	using Enums;

    public class Card : ICard
    {
        public static ConsoleColor[] SuitColors = { ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green };

        public Rank Rank { get; }
        public Suit Suit { get; }

        // Associates a prime number with every rank and suit
        private static int[] rankPrimes = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41 };
        private static int[] suitPrimes = new int[] { 43, 47, 53, 59 };

        // Uses underlying enum type as index to access values in rank-/suitPrimes arrays
        public int PrimeRank { get { return rankPrimes[(int)Rank]; } }
        public int PrimeSuit { get { return suitPrimes[(int)Suit]; } }

        public bool Equals(ICard other)
        {
            return this.Rank == other.Rank && this.Suit == other.Suit;
        }

        // Multiplying prime numbers for rank and suit gives every card a hash code that is guaranteed to be unique
        public int GetHashCode(Card c)
        {
            return c.PrimeRank * c.PrimeSuit;
        }

        // Used two chars as argument for card rank and suit, see switches below
        public Card(string s)
        {
            var chars = s.ToUpper().ToCharArray();
            if (chars.Length != 2) throw new ArgumentException("Card string must be length 2");
            switch (chars[0])
            {
                case '2': this.Rank = Rank.Two; break;
                case '3': this.Rank = Rank.Three; break;
                case '4': this.Rank = Rank.Four; break;
                case '5': this.Rank = Rank.Five; break;
                case '6': this.Rank = Rank.Six; break;
                case '7': this.Rank = Rank.Seven; break;
                case '8': this.Rank = Rank.Eight; break;
                case '9': this.Rank = Rank.Nine; break;
                case 'T': this.Rank = Rank.Ten; break;
                case 'J': this.Rank = Rank.Jack; break;
                case 'Q': this.Rank = Rank.Queen; break;
                case 'K': this.Rank = Rank.King; break;
                case 'A': this.Rank = Rank.Ace; break;
                default: throw new ArgumentException("Card string rank not valid");
            }
            switch (chars[1])
            {
                case 'S': this.Suit = Suit.Spades; break;
                case 'H': this.Suit = Suit.Hearts; break;
                case 'D': this.Suit = Suit.Diamonds; break;
                case 'C': this.Suit = Suit.Clubs; break;
                default: throw new ArgumentException("Card string suit not valid");
            }
        }

        public override string ToString()
        {
            char[] ranks = "23456789TJQKA".ToCharArray();
            char[] suits = { '♠', '♥', '♦', '♣' };

            return ranks[(int)Rank].ToString() + suits[(int)Suit].ToString();
        }
    }
}
