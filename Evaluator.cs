using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Combinatorics;

namespace SnapCall
{
	using System.IO;

	public class Evaluator
	{
		private bool debug;
		private HashMap handRankMap;
		private Lazy<IHandFactory> HandFactory { get; } = new Lazy<IHandFactory>(() => new HandFactory());

		public Evaluator(
			string fileName =	null,
			bool fiveCard =		true,
			bool sixCard =		true,
			bool sevenCard =	true,
			double loadFactor =	1.25,
			bool debug =		true)
		{

			DateTime start = DateTime.UtcNow;
			this.debug = debug;

			if (sixCard && !fiveCard) throw new ArgumentException("Six card eval requires five card eval");
			if (sevenCard && !sixCard) throw new ArgumentException("Seven card eval requires six card eval");

			// Load hand rank table or create one if no filename was given
			if (fileName != null)
			{
				if (!File.Exists(fileName))
				{
					throw new ArgumentException(string.Format("File {0} does not exist", fileName));
				}
				else
				{
					if (debug) Console.WriteLine("Loading table from {0}", fileName);
					LoadFromFile(fileName);
				}
			}
			else
			{
				int minHashMapSize = (fiveCard ? 2598960 : 0) + (sixCard ? 20358520 : 0) + (sevenCard ? 133784560 : 0);
				handRankMap = new HashMap((uint)(minHashMapSize * loadFactor));
				if (fiveCard)
				{
					if (debug) Console.WriteLine("Generating new five card lookup table");
					GenerateFiveCardTable();
				}
				if (sixCard)
				{
					if (debug) Console.WriteLine("Generating new six card lookup table");
					GenerateSixCardTable();
				}
				if (sevenCard)
				{
					if (debug) Console.WriteLine("Generating new seven card lookup table");
					GenerateSevenCardTable();
				}
			}

			TimeSpan elapsed = DateTime.UtcNow - start;
			if (debug) Console.WriteLine("Hand evaluator setup completed in {0:0.00}s", elapsed.TotalSeconds);
		}

		public int Evaluate(ulong bitmap)
		{
			return (int)handRankMap[bitmap];
		}

		public void SaveToFile(string fileName)
		{
			if (debug) Console.WriteLine("Saving table to {0}", fileName);
			using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
			{
				var bytes = HashMap.Serialize(handRankMap);
				fileStream.Write(bytes, 0, bytes.Length);
			}
		}

		private void LoadFromFile(string path)
		{
			using (FileStream inputStream = new FileStream(path, FileMode.Open))
			using (MemoryStream memoryStream = new MemoryStream())
			{
				inputStream.CopyTo(memoryStream);
				handRankMap = HashMap.Deserialize(memoryStream.ToArray());
			}
		}

		private void GenerateFiveCardTable()
		{
			// Generate a list of 52 integers starting from 0 which represent all cards in a deck
			var sourceSet = Enumerable.Range(0, 52).ToList();
			// Generate all combinations of 5 from the 52 integers representing all possible 5 card hands
			var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 5);

			// Generate all possible 5 card hand bitmaps
			Console.WriteLine("Generating bitmaps");
			var handBitmaps = new List<ulong>();
			int count = 0;
			foreach (List<int> values in combinations)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, combinations.Count);
				// Transform the integer card hands into a bitmap by left-shifting 1 by integer value while aggregating with a bitwise OR
				handBitmaps.Add(values.Aggregate(0ul, (acc, el) => acc | (1ul << el)));
			}

			// Calculate hand strength of each hand by generating a Hand object from each bitmap and calling GetStrength
			Console.WriteLine("Calculating hand strength");
			var handStrengths = new Dictionary<ulong, HandStrength>();
			count = 0;
			foreach (ulong bitmap in handBitmaps)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handBitmaps.Count);
                var hand = HandFactory.Value.Create(bitmap);
                handStrengths.Add(bitmap, hand.GetStrength());
			}

			// Generate a list of all unique hand strengths
			Console.WriteLine("Generating equivalence classes");
			var uniqueHandStrengths = new List<HandStrength>();
			count = 0;
			foreach (KeyValuePair<ulong, HandStrength> strength in handStrengths)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handStrengths.Count);
				// BinaryInsert does not insert duplicates
				// It also compares the HandStrengths, calling CompareTo, which also compares by kickers
				Utilities.BinaryInsert<HandStrength>(uniqueHandStrengths, strength.Value);
			}
			Console.WriteLine("{0} unique hand strengths", uniqueHandStrengths.Count);

			// Create a map of hand bitmaps to hand strength indices
			Console.WriteLine("Creating lookup table");
			count = 0;
			foreach (ulong bitmap in handBitmaps)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handBitmaps.Count);
				var hand = HandFactory.Value.Create(bitmap);
				HandStrength strength = hand.GetStrength();
				// Equivalence is the index of the HandStrength in uniqueHandStrengths
				var equivalence = Utilities.BinarySearch<HandStrength>(uniqueHandStrengths, strength);
				if (equivalence == null) throw new Exception(string.Format("{0} hand not found", hand));
				else
				{
					// The hand bitmap is the key, the equivalence (hand strength ranking) is the item
					handRankMap[bitmap] = (ulong) equivalence;
				}
			}
		}

		private void GenerateSixCardTable()
		{
			// TODO: Five card table was start = 0
			var sourceSet = Enumerable.Range(1, 52).ToList();
            // Generate all combinations of 6 from the 52 integers representing all possible 6 card hands
            var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 6);
			int count = 0;
			// For every 6 card hand: Get all 5 card combinations (subsets),
			// aggregate a bitmap for every subset and add its evaluation to subsetValues (from handRankMap (five card table)),
			// then aggregate a bitmap for every 6 card hand and add it with its highest subsetValues evaluation to the handRankMap
			foreach (List<int> cards in combinations)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, combinations.Count);
				var subsets = new Combinatorics.Collections.Combinations<int>(cards, 5);
				var subsetValues = new List<ulong>();
				foreach (List<int> subset in subsets)
				{
					ulong subsetBitmap = subset.Aggregate(0ul, (acc, el) => acc | (1ul << el));
					subsetValues.Add(handRankMap[subsetBitmap]);
				}
				ulong bitmap = cards.Aggregate(0ul, (acc, el) => acc | (1ul << el));
				handRankMap[bitmap] = subsetValues.Max();
			}
		}

		private void GenerateSevenCardTable()
		{
            // TODO: Five card table was start = 0
            var sourceSet = Enumerable.Range(1, 52).ToList();
            // Generate all combinations of 7 from the 52 integers representing all possible 7 card hands
            var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 7);
			int count = 0;
            // For every 7 card hand: Get all 6 card combinations (subsets),
            // aggregate a bitmap for every subset and add its evaluation to subsetValues (from handRankMap (six card table)),
            // then aggregate a bitmap for every 7 card hand and add it with its highest subsetValues evaluation to the handRankMap
            foreach (List<int> cards in combinations)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, combinations.Count);
				var subsets = new Combinatorics.Collections.Combinations<int>(cards, 6);
				var subsetValues = new List<ulong>();
				foreach (List<int> subset in subsets)
				{
					ulong subsetBitmap = subset.Aggregate(0ul, (acc, el) => acc | (1ul << el));
					subsetValues.Add(handRankMap[subsetBitmap]);
				}
				ulong bitmap = cards.Aggregate(0ul, (acc, el) => acc | (1ul << el));
				handRankMap[bitmap] = subsetValues.Max();
			}
		}
	}
}
