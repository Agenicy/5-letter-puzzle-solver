using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _5LetterPuzzleSolver
{
	/// <summary>
	/// find many 5-letter words and each of them doesn't contain the same letter
	/// like 'punch' and 'crowd' is ok
	/// but 'books' is not (has a repetition of 'o')
	/// 'ratio' and 'ocean' is not allowed (has repetitions of 'a' and 'o')
	/// </summary>
	class Program
	{
		public const int cint_letterLength = 5;
		public const float cf_EnglishLettersCount = 26;
		public const float cf_SearchDepth = cf_EnglishLettersCount / cint_letterLength;
		public const float cf_WinValueHeuristic = (int)cf_SearchDepth - 1;
		const bool isNeedToRegenFile = false;

		static void Main(string[] args)
		{
			List<string> space = new List<string>();
			if (isNeedToRegenFile)
			{
				List<string[]> list = new List<string[]>();
				using var reader = new StreamReader(File.OpenRead("../../../../../103976/EnWords.csv"));
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine().Replace("\"", "");
					var values = line.Split(',');

					if (values[0].Length == cint_letterLength)
					{
						bool isRepetition = false;
						for (int i = 0; i < cint_letterLength && !isRepetition; i++)
						{
							for (int j = i + 1; j < cint_letterLength && !isRepetition; j++)
							{
								if (values[0][i] == values[0][j])
									isRepetition = true;

							}
						}

						if (!isRepetition)
						{
							list.Add(values);
							space.Add(values[0]);
						}
					}
				}
				using (var file = new StreamWriter("./TempFile.csv"))
				{
					foreach (var item in list)
					{
						StringBuilder sb = new StringBuilder();
						sb.AppendJoin('\t', item);
						file.WriteLine(sb.ToString());
					}
				}
			}
			else
			{
				using var reader = new StreamReader(File.OpenRead("./TempFile.csv"));
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					var values = line.Split('\t');
					space.Add(values[0]);
				}
			}

			for (int i = 0; i < 10; i++)
			{

				TreeNode rt = new TreeNode(space, "");

				string output;
				while ((output = TreeNode.GetMove(TreeNode.root)) != "")
				{
					Console.WriteLine(output);
					TreeNode.UpdateWithMove(output);
				}
				Console.WriteLine("");
			}

		}
	}

	class TreeNode
	{
		static Random random = new Random();

		TreeNode parent = null;
		List<TreeNode> child = new List<TreeNode>();
		List<string> space = new List<string>();
		readonly string move;
		HashSet<char> chars;

		int int_WordsCount;

		// MCTS values
		bool isLeaf = true;
		bool isEnd = true;
		int int_VisitCount = 0;
		float f_WinCount = 0;
		float value;
		float f_HeuristicValue = 1.0f;
		static HashSet<char> check = new HashSet<char>() { 'a', 'e', 'i', 'o', 'u' };
		const int cint_VisitLimit = 10000;

		public TreeNode(List<string> space, string move, TreeNode parent = null)
		{
			chars = new HashSet<char>();
			foreach (var c in move)
			{
				chars.Add(c);

				if (check.Contains(c))
				{
					f_HeuristicValue -= 0.2f;
				}
			}
			if (parent is null)
				root = this;
			else
			{
				chars.UnionWith(parent.chars);
			}


			this.space = new List<string>(space);
			this.move = move;
			this.parent = parent;

			int_WordsCount = parent is null ? 1 : parent.int_WordsCount + 1;
		}

		public bool isRepetition(HashSet<char> s)
		{
			TreeNode search = this;
			if (search.move == "")
				return false;

			s.IntersectWith(search.chars);

			return (s.Count > 0);
		}


		public static TreeNode root = null;

		public static string GetMove(TreeNode r)
		{
			r.Expand();

			// rollout limit
			for (int i = 0; i < cint_VisitLimit; i++)
			{
				Playout();
			}

			if (r.child.Count == 0)
				return "";

			TreeNode max = null;
			float max_value = -1;
			for (int i = 0; i < r.child.Count; i++)
			{
				var value = r.child[i].int_VisitCount;
				if (value > max_value)
				{
					max = r.child[i];
					max_value = value;
				}
			}

			//return max.move;
			StringBuilder sb = new StringBuilder();
			sb.Append(max.move);
			if (!max.isEnd)
			{
				sb.Append(GetMove(max));
			}
			return sb.ToString();
		}

		static void Playout()
		{
			Playout_OneTime();
		}

		static void Playout_OneTime()
		{
			TreeNode node = root;

			// Selection
			while (!node.isLeaf)
			{
				node = node.Select();
			}


			// virtual loss
			TreeNode expandNode = node;
			expandNode.f_WinCount -= 1;

			// Expansion
			if (!node.isEnd)
			{
				node.Expand();
				node = node.Select();
			}

			// Simulation
			node.value = EvaluateRollout(node.DeepCopy()); // deep copy

			// virtual loss
			expandNode.f_WinCount += 1;

			// Backpropagation
			node.BackPropagate(node.value);
		}

		static float EvaluateRollout(TreeNode node)
		{
			if (node.isEnd)
				return node.isWin();

			for (int i = 0; i < Program.cf_SearchDepth; i++)
			{
				if (node.isLeaf)
					node.Expand();

				if (node.isEnd)
					break;

				node = node.Select();
			}

			// now is leaf, and game over
			return node.isWin();
		}

		public static void UpdateWithMove(string move)
		{
			foreach (var c in root.child)
			{
				if (c.move == move)
				{
					root = c;
					return;
				}
			}
			throw new Exception();
		}

		/////////////////////////////////////////////

		TreeNode DeepCopy()
		{
			TreeNode n = (TreeNode)this.MemberwiseClone();

			n.child = new List<TreeNode>(child);
			n.space = new List<string>(space);

			return n;
		}

		public void Expand()
		{
			if (!isLeaf) return;


			isEnd = true;

			child.Clear();


			foreach (var move in space.ToArray())
			{
				var key = new HashSet<char>();
				foreach (var c in move)
				{
					key.Add(c);
				}

				if (isRepetition(key))
				{
					space.Remove(move);
				}
			}

			if (space.Count > 0)
			{
				isEnd = false;
				isLeaf = false;
			}

			for (int i = 0; i < space.Count; i++)
			{
				var move = space[i];


				var key = new HashSet<char>();
				foreach (var c in move)
				{
					key.Add(c);
				}

				if (isRepetition(key))
				{
					throw new Exception();
				}

				var after_space = new List<string>(space);

				after_space.Remove(move);

				child.Add(new TreeNode(after_space, move, this));

			}
		}

		public TreeNode Select()
		{
			if (child.Count == 0)
				return this;

			//return child[random.Next(0, child.Count)];

			float valueTotal = 0;
			List<float> Dart_Float = new List<float>();

			for (int i = 0; i < child.Count; i++)
			{
				valueTotal += child[i].GetHeuristicValue(); // must > 0
				Dart_Float.Add(valueTotal);
			}

			double dartValue = random.NextDouble() * valueTotal;
			for (int i = 0; i < Dart_Float.Count; i++)
			{
				if (dartValue <= Dart_Float[i])
					return child[i];
			}

			throw new Exception();
		}

		public void Update(float value)
		{
			++int_VisitCount;
			f_WinCount += value;
		}
		void BackPropagate(float value)
		{
			Update(value);
			parent?.BackPropagate(value);
		}

		float GetHeuristicValue()
		{
			if (parent is null)
				return 0;

			return 1 + f_HeuristicValue + (f_WinCount / (1 + (float)int_VisitCount)) + (float)Math.Sqrt(Math.Log(1 + parent.int_VisitCount) / (1 + (float)int_VisitCount));
		}

		int isWin()
		{
			if (isEnd)
				return int_WordsCount > Program.cf_WinValueHeuristic?1:0;
			else
				return 0;
		}
	}
}
