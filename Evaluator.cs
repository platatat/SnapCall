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
		private Dictionary<ulong, ulong> monteCarloMap;

		public Evaluator(
			string fileName =	null,
			bool fiveCard =		true,
			bool sixCard =		true,
			bool sevenCard =	true,
			double loadFactor =	1.25,
			bool debug =		true,
			bool runCustom =	false)
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
			
			// Run custom scripts like monte carlo generation
			if (runCustom)
			{
				Console.WriteLine("Running monte carlo simulation");
				GenerateMonteCarloMap(100000);
				Console.WriteLine("Writing table to disk");
				SaveToFile(fileName);
			}

			TimeSpan elapsed = DateTime.UtcNow - start;
			if (debug) Console.WriteLine("Hand evaluator setup completed in {0:0.00}s", elapsed.TotalSeconds);
		}

		public int Evaluate(ulong bitmap)
		{
			// Check if 2-card monte carlo map has an evaluation for this hand
			//if (monteCarloMap.ContainsKey(bitmap)) return (int)monteCarloMap[bitmap];

			// Otherwise return the real evaluation
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
			var sourceSet = Enumerable.Range(0, 52).ToList();
			var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 5);

			// Generate all possible 5 card hand bitmaps
			Console.WriteLine("Generating bitmaps");
			var handBitmaps = new List<ulong>();
			int count = 0;
			foreach (List<int> values in combinations)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, combinations.Count);
				handBitmaps.Add(values.Aggregate(0ul, (acc, el) => acc | (1ul << el)));
			}

			// Calculate hand strength of each hand
			Console.WriteLine("Calculating hand strength");
			var handStrengths = new Dictionary<ulong, HandStrength>();
			count = 0;
			foreach (ulong bitmap in handBitmaps)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handBitmaps.Count);
				var hand = new Hand(bitmap);
				handStrengths.Add(bitmap, hand.GetStrength());
			}

			// Generate a list of all unique hand strengths
			Console.WriteLine("Generating equivalence classes");
			var uniqueHandStrengths = new List<HandStrength>();
			count = 0;
			foreach (KeyValuePair<ulong, HandStrength> strength in handStrengths)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handStrengths.Count);
				Utilities.BinaryInsert<HandStrength>(uniqueHandStrengths, strength.Value);
			}
			Console.WriteLine("{0} unique hand strengths", uniqueHandStrengths.Count);

			// Create a map of hand bitmaps to hand strength indices
			Console.WriteLine("Creating lookup table");
			count = 0;
			foreach (ulong bitmap in handBitmaps)
			{
				if (debug && count++ % 1000 == 0) Console.Write("{0} / {1}\r", count, handBitmaps.Count);
				var hand = new Hand(bitmap);
				HandStrength strength = hand.GetStrength();
				var equivalence = Utilities.BinarySearch<HandStrength>(uniqueHandStrengths, strength);
				if (equivalence == null) throw new Exception(string.Format("{0} hand not found", hand));
				else
				{
					handRankMap[bitmap] = (ulong) equivalence;
				}
			}
		}

		private void GenerateSixCardTable()
		{
			var sourceSet = Enumerable.Range(1, 52).ToList();
			var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 6);
			int count = 0;
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
			var sourceSet = Enumerable.Range(1, 52).ToList();
			var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 7);
			int count = 0;
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

		private void GenerateMonteCarloMap(int iterations)
		{
			monteCarloMap = new Dictionary<ulong, ulong>();
			var sourceSet = Enumerable.Range(1, 52).ToList();
			var combinations = new Combinatorics.Collections.Combinations<int>(sourceSet, 2);
			int count = 0;
			foreach (List<int> cards in combinations)
			{
				Console.Write("{0}\r", count++);

				ulong bitmap = cards.Aggregate(0ul, (acc, el) => acc | (1ul << el));
				var hand = new Hand(bitmap);
				var deck = new Deck(removedCards: bitmap);

				ulong evaluationSum = 0;
				for (int i = 0; i < iterations; i++)
				{
					if (deck.CardsRemaining < 13) deck.Shuffle();
					evaluationSum += handRankMap[bitmap | deck.Draw(5)];
				}

				monteCarloMap[bitmap] = evaluationSum / (ulong)iterations;
			}

			foreach (KeyValuePair<ulong, ulong> kvp in monteCarloMap.OrderBy(kvp => kvp.Value))
			{
				var hand = new Hand(kvp.Key);
				hand.PrintColoredCards("\t");
				Console.WriteLine(kvp.Value);
				handRankMap[kvp.Key] = kvp.Value;
			}
			Console.ReadLine();
		}
	}
}
