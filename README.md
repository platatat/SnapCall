# SnapCall
Fast C# poker hand evaluator for five to seven cards.

<h2>Overview</h2>
SnapCall is a high performance poker hand evaluation library made for Texas Hold'Em, though it can be used for any poker game with the same hand ranks. High speed lookups are achieved by precomputing all possible hand strengths and storing them in a hash table. The lookup table sizes (using the default load factor) are as follows:

	Five Card: ~25MB
	Six Card: ~300MB
	Siven Card: ~2GB

While the table sizes increase with hand size, the lookup times are constant as long as the whole table fits in memory.

<h2>Performance</h2>
SnapCall provides a massive improvement over python libraries, but still falls short of a C implementation. SnapCall aims to eventually match or surpass the performance of a fast C library - see the Future Development section. Since SnapCall is based entirely on lookup tables, performance doesn't degrade at all as hand size increases.

**Five Card Evals Per Second**

	Pokerhand-Eval (Python) ......... 100
	Deuces (Python) ............. 250,000
	SKPokerEval (C++) ........... 400,000
	SnapCall (C#) ............. 7,500,000
	Poker-Eval (C) ........... 30,000,000

**Seven Card Evals Per Second**

	Pokerhand-Eval (Python) .......... 50
	Deuces (Python) .............. 15,000
	SKPokerEval (C++) ........... 140,000
	SnapCall (C#) ............. 7,500,000
	Poker-Eval (C) ........... 30,000,000

<h2>Usage</h2>

The Evaluator constructor called with no arguments will create a new five card lookup table from scratch, which can then be saved to a file for later reuse. Note that each constructor is inclusive, so the seven card evaluator can evaluate five and six card hands as well.
```c#
var fiveCardEvaluator = new Evaluator();
fiveCardEvaluator.WriteToFile("./eval_tables/five_card.ser");

var sixCardEvaluator = new Evaluator(sixCard: true);
sixCardEvaluator.WriteToFile("./eval_tables/six_card.ser");

var sevenCardEvaluator = new Evaluator(sixCard: true, sevenCard: true);
sevenCardEvaluator.WriteToFile("./eval_tables/seven_card.ser");
```

First time table creation is slow - five card takes about a minute, six card about 10 minutes, and seven card can take over an hour. You'll only want to do this once, and then save the table to a file and reuse it. Loading a seven card table takes about 20 seconds, and five and six cards are less than 5 seconds.

```c#
var fiveCardEvaluator = new Evaluator(fileName: "./eval_tables/five_card.ser");
var sixCardEvaluator = new Evaluator(fileName: "./eval_tables/six_card.ser");
var sevenCardEvaluator = new Evaluator(fileName: "./eval_tables/seven_card.ser");
```

Cards are represented as 64 bit masks, with the most significant 12 bits unused and the remaining 52 bits representing each possible card.

	0x00000001 => 2♠
	0x00000002 => 2♡
	0x00000004 => 2♢
	0x00000008 => 2♣
	0x00000010 => 3♠
	0x00000020 => 3♡
	...

A hand is a bitwise OR of all the cards making up the hand. To evaluate a hand, pass its bitmask (UInt64) to the evaluator, and it will return a number from 0 to 7461 representing the hand's strength equivalence class. If two hands have the same equivalence class, they tie. If the class of hand A is greater than the class of hand B, then A beats B. Each equivalence class can contain many unique hands, but there are only 7462 distinct ranks.

	7461 => A, K, Q, J, T (flush) Ace high straight flush (4 possible hands)
	7460 => K, Q, J, T, 9 (flush) King high straight flush (4 possible hands)
	...
	7452 => 5, 4, 3, 2, A (flush) Five high straight flush (4 possible hands)
	7451 => A, A, A, A, K (no flush) Quad aces, king kicker (4 possible hands)
	...
	0    => 2, 3, 4, 5, 7 (no flush) Seven high (1020 possible hands)

<h2>Dependencies</h2>
SnapCall uses the Combinatorics library for generating bitmaps, and the Net-ProtoBuf library for object serialization. Both are available through NuGet.

<h2>Future Development Goals</h2>
* Remove dependency on Combinatorics package.
* Improve HashMap class perfomance to achieve 30M+ evals per second.
* Be able to evaluate 2, 3, and 4 card hands efficiently.
