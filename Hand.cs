using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
    public class Hand : IHand
    {
        private IList<ICard> Cards { get; }

        public Hand()
        {
            Cards = new List<ICard>();
        }

        public Hand(IEnumerable<ICard> cards)
        {
            Cards = cards.ToList();          
        }

        public Hand(ulong bitmap) : this(bitmap.GetCardsFromBitmap()) { }

        public void PrintColoredCards(string end = "")
        {
            for (int i = 0; i < Cards.Count; i++)
            {
                var card = Cards.ElementAt(i);
                Console.ForegroundColor = Card.SuitColors[(int)card.Suit];
                Console.Write("{0}", card);
                if (i < Cards.Count - 1) Console.Write(" ");
            }
            Console.ResetColor();
            Console.Write(end);
        }

        public override string ToString()
        {
            return string.Join(" ", Cards.Select(card => card.ToString()));
        }

        public HandStrength GetStrength()
        {
            if (Cards.Count == 5)
            {
                var strength = new HandStrength();
                strength.Kickers = new List<int>();

                // Multiplying PrimeRank by 100 ensures that cards are ordered primarily by rank (ascending)
                var orderedCards = Cards.OrderBy(card => card.PrimeRank * 100 + card.PrimeSuit).ToList();

                // Multiply all PrimeRanks/PrimeSuits as rankProduct/suitProduct,
                // then match for known straight/flush products and assign to variable
                int rankProduct = orderedCards.Select(card => card.PrimeRank).Aggregate((acc, r) => acc * r);
                int suitProduct = orderedCards.Select(card => card.PrimeSuit).Aggregate((acc, r) => acc * r);

                bool straight =
                    rankProduct == 8610         // 5-high straight
                    || rankProduct == 2310      // 6-high straight
                    || rankProduct == 15015     // 7-high straight
                    || rankProduct == 85085     // 8-high straight
                    || rankProduct == 323323    // 9-high straight
                    || rankProduct == 1062347   // T-high straight
                    || rankProduct == 2800733   // J-high straight
                    || rankProduct == 6678671   // Q-high straight
                    || rankProduct == 14535931  // K-high straight
                    || rankProduct == 31367009; // A-high straight

                bool flush =
                    suitProduct == 147008443        // Spades
                    || suitProduct == 229345007     // Hearts
                    || suitProduct == 418195493     // Diamonds
                    || suitProduct == 714924299;    // Clubs

                // Group cards by rank then check group counts and assign rank underlying enum value to corresponding count variable
                var cardCounts = orderedCards.GroupBy(card => (int)card.Rank).Select(group => group).ToList();

                var fourOfAKind = -1;
                var threeOfAKind = -1;
                var onePair = -1;
                var twoPair = -1;

                foreach (var group in cardCounts)
                {
                    var rank = group.Key;
                    var count = group.Count();
                    if (count == 4) fourOfAKind = rank;
                    else if (count == 3) threeOfAKind = rank;
                    else if (count == 2)
                    {
                        twoPair = onePair;
                        onePair = rank;
                    }
                }

                // Resolve strength and kickers based on the previously assigned variables
                if (straight && flush)
                {
                    strength.HandRanking = HandRanking.StraightFlush;
                    strength.Kickers = orderedCards.Select(card => (int)card.Rank).Reverse().ToList();
                }
                else if (fourOfAKind >= 0)
                {
                    strength.HandRanking = HandRanking.FourOfAKind;
                    strength.Kickers.Add(fourOfAKind);
                    strength.Kickers.AddRange(orderedCards
                        .Where(card => (int)card.Rank != fourOfAKind)
                        .Select(card => (int)card.Rank));
                }
                else if (threeOfAKind >= 0 && onePair >= 0)
                {
                    strength.HandRanking = HandRanking.FullHouse;
                    strength.Kickers.Add(threeOfAKind);
                    strength.Kickers.Add(onePair);
                }
                else if (flush)
                {
                    strength.HandRanking = HandRanking.Flush;
                    strength.Kickers.AddRange(orderedCards
                        .Select(card => (int)card.Rank)
                        .Reverse());
                }
                else if (straight)
                {
                    strength.HandRanking = HandRanking.Straight;
                    strength.Kickers.AddRange(orderedCards
                        .Select(card => (int)card.Rank)
                        .Reverse());
                }
                else if (threeOfAKind >= 0)
                {
                    strength.HandRanking = HandRanking.ThreeOfAKind;
                    strength.Kickers.Add(threeOfAKind);
                    strength.Kickers.AddRange(orderedCards
                        .Where(card => (int)card.Rank != threeOfAKind)
                        .Select(card => (int)card.Rank));
                }
                else if (twoPair >= 0)
                {
                    strength.HandRanking = HandRanking.TwoPair;
                    strength.Kickers.Add(Math.Max(twoPair, onePair));
                    strength.Kickers.Add(Math.Min(twoPair, onePair));
                    strength.Kickers.AddRange(orderedCards
                        .Where(card => (int)card.Rank != twoPair && (int)card.Rank != onePair)
                        .Select(card => (int)card.Rank));
                }
                else if (onePair >= 0)
                {
                    strength.HandRanking = HandRanking.Pair;
                    strength.Kickers.Add(onePair);
                    strength.Kickers.AddRange(orderedCards
                        .Where(card => (int)card.Rank != onePair)
                        .Select(card => (int)card.Rank));
                }
                else
                {
                    strength.HandRanking = HandRanking.HighCard;
                    strength.Kickers.AddRange(orderedCards
                        .Select(card => (int)card.Rank)
                        .Reverse());
                }

                return strength;
            }
            else
            {
                return null;
            }
        }

        public IEnumerator<ICard> GetEnumerator()
        {
            return Cards.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Cards).GetEnumerator();
        }
    }
}
