using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MicroLisp
{
	public class EvalException : Exception
	{
		public EvalException()
		{
		}

		public EvalException(string message)
			: base(message)
		{
		}

		public EvalException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}

	public class Primitives
	{
		public static string[] names = new string[]
		{
			"if",
			"quote",
			"set!",
			"define",
			"initialize",
			"lambda",
			"begin",
			"+",
			"-",
			"*",
			"/",
			"not",
			"and",
			"or",
			">",
			"<",
			">=",
			"<=",
			"=",
			"equal?",
			"eq?",
			"length",
			"cons",
			"car",
			"cdr",
			"append",
			"list",
			"list?",
			"null?",
			"symbol?",
			"defined?",
			"modulo",
			"abs",
			"floor",
			"ceiling",
			"min",
			"max",
			"apply",
			"id",
			"sqrt",
			"let",
			"pow",
			"ln",
			"sin",
			"cos",
			"tan",
			"asin",
			"acos",
			"atan2"
		};

		public enum Names
		{
			If,
			Quote,
			Set,
			Define,
			Initialize,
			Lambda,
			Begin,
			Plus,
			Minus,
			Mult,
			Div,
			Not,
			And,
			Or,
			GT,
			LT,
			GTEQ,
			LTEQ,
			EQ,
			EQ2,
			EQref,
			Length,
			Cons,
			Car,
			Cdr,
			Append,
			MakeList,
			IsList,
			IsNull,
			IsSymbol,
			IsDefined,
			Mod,
			Abs,
			Floor,
			Ceil,
			Min,
			Max,
			Apply,
			ID,
			Sqrt,
			Let,
			Pow,
			Ln,
			Sin,
			Cos,
			Tan,
			Asin,
			Acos,
			Atan2
		};
	}

	public class LispValue
	{
		public override string ToString()
		{
			return "<invalidValue>";
		}
	}
	public class LispAtom : LispValue
	{
		public int primitiveIndex = -1;

		public LispAtom()
		{
		}

		public LispAtom(Primitives.Names name)
		{
			primitiveIndex = (int) name;
		}

		public override string ToString()
		{
			if (primitiveIndex >= 0)
				return Primitives.names[primitiveIndex];
			else
				return "<invalidAtom>";
		}

		public bool ReadFrom(string s)
		{
			primitiveIndex = Array.IndexOf (Primitives.names, s);
			return (primitiveIndex >= 0);
		}
	}
	/*public class LispString : LispValue
	{
		public string s;
		
		public LispString ()
		{
			s = "";
		}
		
		public LispString (string str)
		{
			s = str;
		}
		
		public override string ToString()
		{
			return '"' + s + '"';
		}
	}*/
	public class LispSymbol : LispValue
	{
		public string s;
		
		public LispSymbol ()
		{
			s = "";
		}
		
		public LispSymbol (string str)
		{
			s = str;
		}
		
		public override string ToString()
		{
			return s;
		}
	}
	public class LispList : LispValue
	{
		public List<LispValue> list;
	
		public LispList()
		{
			list = new List<LispValue>();
		}

		public override string ToString ()
		{
			string result = "(";
			for (int i = 0; i < list.Count; i++) {
				if (i > 0) result += ' ';
				result += list[i].ToString ();
			}
			result += ')';
			return result;
		}
	}
	/*class LispDottedList : LispValue
	{
		public List<LispValue> list;
		public LispValue last;

		public LispDottedList()
		{
			list = new List<LispValue>();
		}

		public override string ToString()
		{
			return '(' + list.ToString() + " . " + last.ToString() + ')';
		}
	}*/
	public class LispNumber : LispValue
	{
		public double n;

		public LispNumber()
		{
		}

		public LispNumber(double val)
		{
			n = val;
		}

		public override string ToString()
		{
			return n.ToString();
		}

		public bool ReadFrom(string s)
		{
			bool result = true;
			try 
			{
				n = Convert.ToSingle (s);
			}
			catch (FormatException e)
			{
				result = false;
			}
			catch (OverflowException e)
			{
				result = false;
			}
			return result;
		}
	}
	public class LispBool : LispValue
	{
		public bool b;

		public LispBool()
		{
		}

		public LispBool(bool val)
		{
			b = val;
		}

		public override string ToString()
		{
			return (b ? "#t" : "#f");
		}

		public bool ReadFrom(string s)
		{
			if (s == "#t")
			{
				b = true;
				return true;
			}
			else if (s == "#f")
			{
				b = false;
				return true;
			}
			else
			{
				return false;
			}
		}
	}
	// LispFunc doesn't get created by the parser, only the evaluator
	public class LispFunc : LispValue
	{
		// all the stuff we need for later evaluation
		public Environment env;
		public List <String> variables;
		public LispValue expression;

		public override string ToString ()
		{
			string result = "(lambda (";
			for (int i = 0; i < variables.Count; i++)
			{
				if (i != 0)
					result += ' ';
				result += variables[i];
			}
			result += ") " + expression.ToString() + ")";

			return result;
		}
	}

	// never created by the parser, only added externally by the host application
	public class ExternalFunc : LispValue
	{
		public delegate LispValue CFunc(List<LispValue> args);
		public CFunc func;

		public ExternalFunc(CFunc newfunc)
		{
			func = newfunc;
		}

		public override string ToString ()
		{
			return "<ExternalFunc>";
		}
	}

	public class ParseResult
	{
		public LispValue value;
		public string error;

		public ParseResult()
		{
			value = new LispValue();
			error = "";
		}

		public ParseResult(LispValue v, string e)
		{
			value = v;
			error = e;
		}

		public void addError(string e)
		{
			if (error != "")
			{
				error += '\n';
			}
			error += e;
		}
	}

	public class Environment
	{
		private Environment outer = null;
		private Dictionary <String,LispValue> mapping = null;

		public Environment (List <String> parms = null, List <LispValue> args = null, Environment newOuter = null)
		{
			outer = newOuter;

			mapping = new Dictionary<string, LispValue> ();

			if (parms != null && args != null) {
				int count = Math.Min (parms.Count, args.Count);
				for (int i = 0; i < count; i++) {
					mapping.Add (parms [i], args [i]);
				}
			}
		}

		public bool TryGetValue(string key, out LispValue value)
		{
			if (mapping.TryGetValue (key, out value))
			{
				return true;
			}
			else
			{
				if (outer == null)
					return false;
				else
					return outer.TryGetValue(key, out value);
			}
		}

		public void Add(string key, LispValue value)
		{
			// this behavior forbids redifining an already defined symbol (throws an argument exception)
			//mapping.Add (key, value);

			// this behavior allows redefining an already defined symbol, which is usually allowed by lisp
			mapping [key] = value;
		}

		// only add it if it doesn't exist, otherwise ignore it entirely
		// returns true if it was added
		public bool Initialize(string key, LispValue value)
		{
			if (!Contains(key))
			{
				Add(key, value);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool Set(string key, LispValue value)
		{
			if (mapping.ContainsKey (key))
			{
				mapping[key] = value;
				return true;
			}
			else if (outer != null)
			{
				return outer.Set (key,value);
			}
			else
			{
				return false;
			}
		}

		public bool Contains(string key)
		{
			if (mapping.ContainsKey (key))
			{
				return true;
			}
			else if (outer != null)
			{
				return outer.Contains (key);
			}
			else
			{
				return false;
			}
		}

		// search recursively through the lispvalue adding any found evironments to the outEnv list
		static private void FindEnvironments(LispValue value, List<Environment> outEnv)
		{
			// lists and functions can have child values, recurse into them
			if (value is LispList)
			{
				foreach (var element in (value as LispList).list)
				{
					// recurse
					FindEnvironments(element, outEnv);
				}
			}
			else if (value is LispFunc)
			{
				// recurse into the function's environment and its expression
				LispFunc f = value as LispFunc;
				f.env.FindSubEnvironments(outEnv);
				FindEnvironments(f.expression, outEnv);
			}
		}

		// N^2 due to naive search for existing environments
		private void FindSubEnvironments(List<Environment> outEnv)
		{
			bool unique = true;
			foreach (var env in outEnv)
			{
				if (System.Object.ReferenceEquals(this, env))
				{
					unique = false;
				}
			}

			// if it's a new environment, add it to the list and recurse
			if (unique)
			{
				outEnv.Add(this);
				foreach (var pair in mapping)
				{
					FindEnvironments(pair.Value, outEnv);
				}
			}
		}

		// returns -1 if it can't be found
		static int GetEnviromentIndex(List<Environment> uniqueEnvironments, Environment env)
		{
			int index = 0;
			foreach (var element in uniqueEnvironments)
			{
				if (System.Object.ReferenceEquals(env, element))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public string Serialize(List<Environment> uniqueEnvironments)
		{
			string result = "";

			int thisIndex = Environment.GetEnviromentIndex(uniqueEnvironments, this);
			if (thisIndex < 0)
			{
				throw new Exception("can't find environment for sub-environment");
			}
			int outerIndex = outer == null ? -1 : Environment.GetEnviromentIndex(uniqueEnvironments, outer);
			result += "(!SETENV! " + thisIndex + " " + outerIndex + "\n";
			foreach (var pair in mapping)
			{
				if (pair.Value is ExternalFunc)
				{
					// nada
				}
				else
				{
					if (pair.Value is LispFunc)
					{
						// serialization of environment
						int environmentIndex = Environment.GetEnviromentIndex(uniqueEnvironments, (pair.Value as LispFunc).env);
						if (environmentIndex < 0)
						{
							throw new Exception("can't find environment for function: (define " + pair.Key + " " + pair.Value.ToString() + ")");
						}

						result += "(!FUNCENV! " + environmentIndex + "\n";
					}
					result += "(define " + pair.Key + " " + pair.Value.ToString() + ")\n";
					if (pair.Value is LispFunc)
					{
						result += ")\n";
					}
				}
			}
			result += ")\n";
			return result;
		}

		// N^2 due to naive search for existing environments
		public override string ToString()
		{
			// first, collect all the environments, including this one, into a unique list
			List<Environment> uniqueEnvironments = new List<Environment>();
			FindSubEnvironments(uniqueEnvironments);

			// now write them all out
			string result = "(!ENVCOUNT! " + uniqueEnvironments.Count + "\n";
			foreach (var env in uniqueEnvironments)
			{
				result += env.Serialize(uniqueEnvironments);
			}
			result += ")";
			return result;
		}

		public void Deserialize(List<Environment> environments, LispValue values, int myIndex)
		{
			if (!(values is LispList)) throw new Exception("Environment deserialization expected a value list");
			List<LispValue> valueList = (values as LispList).list;
			if (valueList.Count < 3) throw new Exception("Environment deserialization expected at least !SETENV! <number> <number> ...");
			if (!(valueList[0] is LispSymbol) || (valueList[0] as LispSymbol).s != "!SETENV!") throw new Exception("Environment deserialization expected !SETENV!");
			if (!(valueList[1] is LispNumber) || !(valueList[2] is LispNumber)) throw new Exception("Environment deserialization expected !SETENV! <number> <number> ...");
			if ((int)(valueList[1] as LispNumber).n != myIndex) throw new Exception("Environment deserialization environment index mismatch: expected " + myIndex + ", got " + (valueList[1] as LispNumber).n);

			// setup outer
			int outerIndex = (int)(valueList[2] as LispNumber).n;
			if (outerIndex == -1)
			{
				outer = null;
			}
			else
			{
				outer = environments[outerIndex];
			}

			for (int i = 3; i < valueList.Count; i++)
			{
				LispValue line = valueList[i];
				if (!(line is LispList)) throw new Exception("Environment deserialization expected a list: " + line.ToString());
				List<LispValue> define = (line as LispList).list;
				if (define.Count != 3) throw new Exception("Environment deserialization expected a list of 3 elements: " + define.ToString());

				if (define[0] is LispAtom)
				{
					if ((define[0] as LispAtom).primitiveIndex != (int)Primitives.Names.Define) throw new Exception("Environment deserialization expected <define> atom: " + define.ToString());
					if (!(define[1] is LispSymbol)) throw new Exception("Environment deserialization expected define symbol: " + define.ToString());
					mapping[(define[1] as LispSymbol).s] = define[2];
				}
				else if (define[0] is LispSymbol)
				{
					// process !FUNCENV! <envidx> block
					if ((define[0] as LispSymbol).s != "!FUNCENV!") throw new Exception("Environment deserialization expected !FUNCENV!: " + define.ToString());
					if (!(define[1] is LispNumber)) throw new Exception("Environment deserialization expected !FUNCENV! <number>: " + define.ToString());
					int funcEnvIndex = (int)(define[1] as LispNumber).n;
					if (funcEnvIndex < 0 || funcEnvIndex >= environments.Count) throw new Exception("Environment deserialization !FUNCENV! <number> is out of range: " + define.ToString());
					if (!(define[2] is LispList)) throw new Exception("Environment deserialization expected !FUNCENV! <number> <list>: " + define.ToString());

					// start building the output func
					LispFunc func = new LispFunc();
					func.env = environments[funcEnvIndex];
					func.variables = new List<string>();

					// process the define block
					List<LispValue> realDefine = (define[2] as LispList).list;
					if (realDefine.Count != 3) throw new Exception("Environment deserialization for lambda expected 3 items <define> <symbol> <expression>: " + realDefine.ToString());
					if (!(realDefine[0] is LispAtom) || (realDefine[0] as LispAtom).primitiveIndex != (int)Primitives.Names.Define) throw new Exception("Environment deserialization for lambda expected <define> atom: " + realDefine.ToString());
					if (!(realDefine[1] is LispSymbol)) throw new Exception("Environment deserialization for lambda expected <define> <symbol>: " + realDefine.ToString());
					string symbol = (realDefine[1] as LispSymbol).s;
					if (!(realDefine[2] is LispList)) throw new Exception("Environment deserialization for lambda expected <define> <symbol> <expression>: " + realDefine.ToString());

					// process the lambda block
					List<LispValue> lambdaList = (realDefine[2] as LispList).list;
					if (lambdaList.Count != 3) throw new Exception("Environment deserialization for lambda expected <lambda> <symbol> <expression>: " + lambdaList.ToString());
					if (!(lambdaList[0] is LispAtom) || (lambdaList[0] as LispAtom).primitiveIndex != (int)Primitives.Names.Lambda) throw new Exception("Environment deserialization for lambda expected <lambda> atom: " + lambdaList.ToString());
					if (!(lambdaList[1] is LispList)) throw new Exception("Environment deserialization for lambda expected <lambda> (arglist): " + lambdaList.ToString());

					// pull variable list out of lambdaList[1]
					List<LispValue> argValueList = (lambdaList[1] as LispList).list;
					foreach (var val in argValueList)
					{
						if (!(val is LispSymbol)) throw new Exception("Environment deserialization for lambda expected <lambda> (arglist) where arglist is a list of symbols: " + lambdaList.ToString());
						func.variables.Add((val as LispSymbol).s);
					}

					// pull expression from lambdaList[2]
					func.expression = lambdaList[2];

					// add it to the environment
					mapping[symbol] = func;
				}
				else
				{
					throw new Exception("Environment deserialization expected <define> atom or !FUNCENV!: " + define.ToString());
				}
			}
		}

		// throws on error
		public void FromString(string str)
		{
			mapping.Clear();
			outer = null;

			MicroLisp helper = new MicroLisp();
			ParseResult result = helper.Parse(str);
			if (result.error != "")
			{
				throw new Exception(result.error);
			}
			else
			{
				if (result.value is LispList)
				{
					List<LispValue> l = (result.value as LispList).list;
					if (l[0] is LispSymbol && (l[0] as LispSymbol).s == "!ENVCOUNT!")
					{
						if (l[1] is LispNumber)
						{
							// create the list of environments
							int envCount = (int) (l[1] as LispNumber).n;
							List<Environment> envs = new List<Environment>();
							envs.Add(this);
							for (int i = 1; i < envCount; i++)
							{
								envs.Add(new Environment());
							}

							// deserialize each environment
							if (envCount == l.Count - 2)
							{
								for (int i = 0; i < envCount; i++)
								{
									envs[i].Deserialize(envs, l[i+2], i);
								}
							}
							else
							{
								throw new Exception("Environment deserialization expected ENVCOUNT " + envCount + " to match elements " + (l.Count - 2));
							}
						}
						else
						{
							throw new Exception("Environment deserialization expected !ENVCOUNT! <count>: " + l.ToString());
						}
					}
					else
					{
						throw new Exception("Environment deserialization expected !ENVCOUNT!");
					}
				}
				else
				{
					throw new Exception("Environment deserialization expected a list");
				}
			}
		}
	}

	public class MicroLisp
	{
		public Environment globalEnv = null;

		public MicroLisp ()
		{
			globalEnv = new Environment();
		}

		public MicroLisp (Environment env)
		{
			globalEnv = env;
			if (env == null) throw new ArgumentNullException("MicroLisp was initialized without an environment");
		}

		public ParseResult ReadFrom (List<string> tokens)
		{
			if (tokens.Count == 0)
				return new ParseResult(new LispValue (), "unexpected end of file");

			string token = tokens [0];
			tokens.RemoveAt (0);

			if (token == "(") 
			{
				LispList list = new LispList();
				ParseResult result = new ParseResult(list,"");
				while (tokens.Count > 0 && tokens[0] != ")")
				{
					ParseResult parsed = ReadFrom (tokens);
					if (parsed.error != "")
					{
						result.addError(parsed.error);
					}
					else
					{
						list.list.Add(parsed.value);
					}
				}
				if (tokens.Count == 0) return new ParseResult(new LispValue(), "unmatched paren: " + (list.list.Count > 0 ? list.list[list.list.Count-1].ToString() : ""));
				tokens.RemoveAt (0); // remove the )
				return result;
			} 
			else if (token == ")") 
			{
				return new ParseResult(new LispValue(), "unexpected \")\"");
			} 
			else 
			{
				LispBool lb = new LispBool();
				LispNumber ln = new LispNumber();
				LispAtom la = new LispAtom();
				if (lb.ReadFrom (token))
				{
					return new ParseResult(lb,"");
				}
				else if (ln.ReadFrom (token))
				{
					return new ParseResult(ln,"");
				}
				else if (la.ReadFrom(token))
				{
					return new ParseResult(la,"");
				}
				else
				{
					return new ParseResult(new LispSymbol(token),"");
				}
			}
		}

		public ParseResult Parse (string line)
		{
			string pattern = ";.*\\n";
			string replacement = "\n";
			Regex rgx = new Regex(pattern);
			line = rgx.Replace(line+"\n", replacement);

			string padded = line.Replace ("(", " ( ").Replace (")"," ) ");
			char[] delimiterChars = { ' ', '\n', '\t', '\r', '\f', '\v', '\0'};
			string[] tokens = padded.Split (delimiterChars, StringSplitOptions.RemoveEmptyEntries);
			List<string> tokenList = new List<string>(tokens);
			ParseResult result = ReadFrom (tokenList);
			if (tokenList.Count != 0)
			{
				result.addError ("unexpected tokens found at end of program:");
				foreach (string token in tokenList)
				{
					result.addError(token);
				}
			}
			return result;
		}

		public delegate TResult FuncWorkaround<T, TResult>(T arg);
		public delegate TResult FuncWorkaround<T1, T2, TResult>(T1 arg1, T2 arg2);
		public delegate TResult FuncWorkaround<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
		public delegate TResult FuncWorkaround<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

		private LispValue NumericBinop(FuncWorkaround<double,double,double> f, List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count != 2) throw new EvalException(prettyOpName + " requires two arguments");

			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);

			if (args[0] is LispNumber && args[1] is LispNumber)
			{
				return new LispNumber(f((args[0] as LispNumber).n, (args[1] as LispNumber).n));
			}
			else
			{
				LispValue args0 = args[0];
				LispValue args1 = args[1];
				throw new EvalException(prettyOpName + " requires two numeric arguments");
			}
		}

		private LispValue NumericMultiop(FuncWorkaround<List<double>,double> f, List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count < 2) throw new EvalException(prettyOpName + " requires at least two arguments");

			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);

			bool non_numeric = false;
			List<double> numbers = new List<double>();
			foreach (LispValue arg in args)
			{
				if (arg is LispNumber)
				{
					numbers.Add((arg as LispNumber).n);
				}
				else
				{
					non_numeric = true;
				}
			}

			if (non_numeric)
			{
				throw new EvalException(prettyOpName + " requires all numeric arguments");
			}
			else
			{
				return new LispNumber(f(numbers));
			}
		}

		private LispValue NumericUnary(FuncWorkaround<double,double> f, List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count != 1) throw new EvalException(prettyOpName + " requires an argument");
			
			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);
			
			if (args[0] is LispNumber)
			{
				return new LispNumber(f((args[0] as LispNumber).n));
			}
			else
			{
				throw new EvalException(prettyOpName + " requires a numeric argument");
			}
		}

		private LispValue ComparisonBinop(FuncWorkaround<double,double,bool> f, List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count != 2) throw new EvalException(prettyOpName + " requires two arguments");

			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);

			if (args[0] is LispNumber && args[1] is LispNumber)
			{
				return new LispBool(f((args[0] as LispNumber).n, (args[1] as LispNumber).n));
			}
			else
			{
				LispValue args0 = args[0];
				LispValue args1 = args[1];
				throw new EvalException(prettyOpName + " requires two numeric arguments");
			}
		}

		private LispValue BoolComparisonBinop(FuncWorkaround<bool,bool,bool> f, List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count != 2) throw new EvalException(prettyOpName + " requires two arguments");
			
			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);
			
			if (args[0] is LispBool && args[1] is LispBool)
			{
				return new LispBool(f((args[0] as LispBool).b, (args[1] as LispBool).b));
			}
			else
			{
				LispValue args0 = args[0];
				LispValue args1 = args[1];
				throw new EvalException(prettyOpName + " requires two boolean arguments");
			}
		}

		private LispValue CompareEquality(List<LispValue> unevaluatedArgs, Environment env, string prettyOpName)
		{
			if (unevaluatedArgs.Count != 2) throw new EvalException(prettyOpName + " requires two arguments");
			
			List<LispValue> args = EvaluateArgs (unevaluatedArgs, env);
			LispValue args0 = args[0];
			LispValue args1 = args[1];

			if (args[0] is LispNumber && args[1] is LispNumber)
			{
				return new LispBool((args[0] as LispNumber).n == (args[1] as LispNumber).n);
			}
			if (args[0] is LispSymbol && args[1] is LispSymbol)
			{
				return new LispBool((args[0] as LispSymbol).s == (args[1] as LispSymbol).s);
			}
			else if (!args0.GetType ().IsAssignableFrom(args1.GetType ()))
			{
				return new LispBool(false);
			}
			else
			{
				throw new EvalException(prettyOpName + ": unimplemented type combination");
			}
		}

		private List <LispValue> EvaluateArgs(List<LispValue> unevaluatedArgs, Environment env)
		{
			List<LispValue> args = new List<LispValue>();
			foreach (LispValue argToEval in unevaluatedArgs)
			{
				args.Add (Eval(argToEval,env));
			}
			return args;
		}

		double Plus(List<double> numbers)
		{
			double result = 0;
			foreach (double number in numbers)
			{
				result += number;
			}
			return result;
		}
		double Minus(List<double> numbers)
		{
			double result = numbers[0];
			for (int i = 1; i < numbers.Count; i++)
			{
				result -= numbers[i];
			}
			return result;
		}
		double Mult(List<double> numbers)
		{
			double result = numbers[0];
			for (int i = 1; i < numbers.Count; i++)
			{
				result *= numbers[i];
			}
			return result;
		}
		double Div(List<double> numbers)
		{
			double result = numbers[0];
			for (int i = 1; i < numbers.Count; i++)
			{
				if (numbers[i] == 0) return 0; // avoid divide by zero
				result /= numbers[i];
			}
			return result;
		}
		double Mod(double a, double b)
		{
			return a % b;
		}
		double Min(List<double> numbers)
		{
			double result = numbers[0];
			for (int i = 1; i < numbers.Count; i++)
			{
				result = Math.Min(result,numbers[i]);
			}
			return result;
		}
		double Max(List<double> numbers)
		{
			double result = numbers[0];
			for (int i = 1; i < numbers.Count; i++)
			{
				result = Math.Max(result,numbers[i]);
			}
			return result;
		}
		bool AND(bool a, bool b)
		{
			return a && b;
		}
		bool OR(bool a, bool b)
		{
			return a || b;
		}
		bool LT(double a, double b)
		{
			return a < b;
		}
		bool GT(double a, double b)
		{
			return a > b;
		}
		bool LTEQ(double a, double b)
		{
			return a <= b;
		}
		bool GTEQ(double a, double b)
		{
			return a >= b;
		}
		bool EQ(double a, double b)
		{
			return (a == b);
		}
		double Abs(double a)
		{
			return Math.Abs (a);
		}
		double Sqrt(double a)
		{
			return Math.Sqrt (a);
		}
		double Floor(double a)
		{
			return (double)Math.Floor (a);
		}
		double Ceil(double a)
		{
			return (double)Math.Ceiling (a);
		}

		LispValue EvalAtom(LispValue x, LispAtom a, List <LispValue> list, Environment env)
		{
			// run the built-in functions
			if (a.primitiveIndex < 0) throw new EvalException("invalid atom");
			List <LispValue> args = new List<LispValue>(list);
			args.RemoveAt (0);
			Primitives.Names type = (Primitives.Names) a.primitiveIndex;
			switch (type)
			{
			case Primitives.Names.If:
				if (args.Count != 3) throw new EvalException("if requires three arguments");
				LispValue test = args[0];
				LispValue conseq = args[1];
				LispValue alt = args[2];
				LispValue testResult = Eval (test, env);
				bool testIsTrue;
				if (testResult is LispBool)
				{
					testIsTrue = (testResult as LispBool).b;
				}
				else
				{
					// by default, all other results evaluate to true
					testIsTrue = true;
				}
				return Eval (testIsTrue ? conseq : alt, env);
			case Primitives.Names.Quote:
				if (args.Count != 1) throw new EvalException("quote requires a single argument");
				return args[0];
			case Primitives.Names.Set:
				if (args.Count != 2) throw new EvalException("set! requires two arguments");
				if (args[0] is LispSymbol)
				{
					string symbolName = (args[0] as LispSymbol).s;
					
					if (!env.Set (symbolName, Eval(args[1], env)))
					{
						throw new EvalException("unknown symbol: " + symbolName);
					}
					// no return value ... ?
				}
				else
				{
					throw new EvalException("the first argument to set! must be a symbol");
				}
				break;
			case Primitives.Names.Initialize:
				goto case Primitives.Names.Define;
			case Primitives.Names.Define:
				if (args.Count < 2) throw new EvalException("define requires at least two arguments");

				// transform (define x (foo) (bar)) into (define x (begin (foo) (bar)))
				LispValue arg1 = args[1];
				if (args.Count > 2)
				{
					LispList newArg1 = new LispList();
					newArg1.list.Add(new LispAtom(Primitives.Names.Begin));
					for (int i = 1; i < args.Count; i++)
					{
						newArg1.list.Add(args[i]);
					}
					arg1 = newArg1;
				}

				if (args[0] is LispSymbol)
				{
					string symbolName = (args[0] as LispSymbol).s;
					try
					{
						LispBool result = new LispBool(true);
						if (type == Primitives.Names.Define)
						{
							env.Add (symbolName, Eval(arg1, env));
						}
						else
						{
							result.b = env.Initialize(symbolName, Eval(arg1, env));
						}
						return result;
					}
					catch (ArgumentException e)
					{
						//throw new EvalException("symbol already defined: " + symbolName);

						// this shouldn't happen anymore now that we allow redefining symbols
						throw new EvalException("error defining symbol: " + symbolName);
					}
				}
				else if (args[0] is LispList)
				{
					List <LispValue> defineArgList = (args[0] as LispList).list;
					if (defineArgList.Count < 1) throw new EvalException("if the first argument to define is a list, you must specify at least a function name and one argument");
					if (defineArgList[0] is LispAtom) throw new EvalException("can't redefine built-in function: " + defineArgList[0].ToString());
					if (!(defineArgList[0] is LispSymbol)) throw new EvalException("the define argument list must consist of symbols: " + defineArgList[0].ToString());

					// transform (define (f x y ...) exp) into (define f (lambda (x y ...) exp)) and evaluate

					LispList newDefineList = new LispList();
					newDefineList.list.Add (new LispAtom(Primitives.Names.Define));
					newDefineList.list.Add (new LispSymbol((defineArgList[0] as LispSymbol).s));

					LispList newLambdaList = new LispList();
					newLambdaList.list.Add (new LispAtom(Primitives.Names.Lambda));
					LispList lambdaArgs = new LispList();
					lambdaArgs.list.AddRange (defineArgList);
					lambdaArgs.list.RemoveAt (0);
					newLambdaList.list.Add (lambdaArgs);
					newLambdaList.list.Add (arg1);

					newDefineList.list.Add (newLambdaList);

					Eval (newDefineList, env);
					return newDefineList;
				}
				else
				{
					throw new EvalException("the first argument to define must be a symbol or symbol list");
				}
				break;
			case Primitives.Names.Lambda:
				if (args.Count != 2) throw new EvalException("lambda requires two arguments");
				LispFunc f = new LispFunc();
				f.env = env;
				f.expression = args[1];
				
				// construct a list of string arguments
				if (args[0] is LispList)
				{
					f.variables = new List<string>();
					
					foreach (LispValue arg in (args[0] as LispList).list)
					{
						if (arg is LispSymbol)
						{
							f.variables.Add ((arg as LispSymbol).s);
						}
						else
						{
							throw new EvalException("unexpected value in lambda's argument list: " + arg.ToString ());
						}
					}
					
					if (f.variables.Count <= 0)
					{
						//throw new EvalException("lambda's argument list must consist of one or more symbols");
					}
				}
				else
				{
					throw new EvalException("the first argument to lambda must be an argument list");
				}
				return f;
			case Primitives.Names.Begin:
				{
					LispValue result = new LispValue();
					foreach (LispValue exp in args)
					{
						result = Eval(exp, env);
					}
					return result;
				}
			case Primitives.Names.Plus:
				return NumericMultiop(Plus,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Minus:
				return NumericMultiop(Minus,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Mult:
				return NumericMultiop(Mult,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Div:
				return NumericMultiop(Div,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Not:
				if (args.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires a single argument");
				LispValue evaluatedArg = Eval (args[0], env);
				bool val;
				if (evaluatedArg is LispBool)
				{
					val = (evaluatedArg as LispBool).b;
				}
				else
				{
					// any other type evaluates to true
					val = true;
				}
				return new LispBool(!val);
			case Primitives.Names.And:
				return BoolComparisonBinop(AND,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Or:
				return BoolComparisonBinop(OR,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.LT:
				return ComparisonBinop(LT,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.GT:
				return ComparisonBinop(GT,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.LTEQ:
				return ComparisonBinop(LTEQ,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.GTEQ:
				return ComparisonBinop(GTEQ,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.EQ:
				return CompareEquality(args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.EQ2:
				goto case Primitives.Names.EQ;
			case Primitives.Names.EQref:
				if (args.Count != 2) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires two arguments");
				return new LispBool(System.Object.ReferenceEquals(args[0],args[1]));
			case Primitives.Names.Length:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1 || !(evaluatedArgs[0] is LispList)) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires a list argument");
				return new LispNumber((evaluatedArgs[0] as LispList).list.Count);
			}
			case Primitives.Names.Cons:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 2) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires two arguments");
				if (evaluatedArgs[1] is LispList)
				{
					LispList newList = new LispList();
					newList.list.Add(evaluatedArgs[0]);
					newList.list.AddRange ((evaluatedArgs[1] as LispList).list);
					return newList;
				}
				else
				{
					LispList newList = new LispList();
					newList.list.Add(evaluatedArgs[0]);
					newList.list.Add(evaluatedArgs[1]);
					return newList;
				}
			}
			case Primitives.Names.Car:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires a list argument");
				if (evaluatedArgs[0] is LispList)
				{
					if ((evaluatedArgs[0] as LispList).list.Count == 0) 
						return new LispList();
					else
						return (evaluatedArgs[0] as LispList).list[0];
				}
				else
				{
					// special case for single non-list argument: just return the argument
					return evaluatedArgs[0];
				}
			}
			case Primitives.Names.Cdr:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires a list argument");
				if (evaluatedArgs[0] is LispList)
				{
					/*// special case for two element lists (pairs): just return the second element
					if ((evaluatedArgs[0] as LispList).list.Count == 2)
					{
						return (evaluatedArgs[0] as LispList).list[1];
					}*/
	
					LispList newList = new LispList();
					if ((evaluatedArgs[0] as LispList).list.Count > 0)
					{
						newList.list.AddRange((evaluatedArgs[0] as LispList).list);
						newList.list.RemoveAt (0);
					}
					
					return newList;
				}
				else
				{
					// special case for single non-list argument: return nil
					return new LispList();
				}
			}
			case Primitives.Names.Append:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 2 || !(evaluatedArgs[0] is LispList) || !(evaluatedArgs[1] is LispList)) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires two list arguments");
				LispList newList = new LispList();
				newList.list.AddRange((evaluatedArgs[0] as LispList).list);
				newList.list.AddRange((evaluatedArgs[1] as LispList).list);
				return newList;
			}
			case Primitives.Names.MakeList:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				LispList newList = new LispList();
				newList.list.AddRange(evaluatedArgs);
				return newList;
			}
			case Primitives.Names.IsList:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires an argument");
				return new LispBool(evaluatedArgs[0] is LispList);
			}
			case Primitives.Names.IsNull:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires an argument");
				return new LispBool((evaluatedArgs[0] is LispList) && (evaluatedArgs[0] as LispList).list.Count == 0);
			}
			case Primitives.Names.IsSymbol:
			{
				List<LispValue> evaluatedArgs = EvaluateArgs (args, env);
				if (evaluatedArgs.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires an argument");
				return new LispBool(evaluatedArgs[0] is LispSymbol);
			}
			case Primitives.Names.IsDefined:
			{
				if (args.Count != 1) throw new EvalException(Primitives.names[a.primitiveIndex] + " requires an argument");
				if (!(args[0] is LispSymbol)) throw new EvalException(Primitives.names[a.primitiveIndex] + " takes a symbol as argument: " + args[0].ToString());
				return new LispBool(env.Contains((args[0] as LispSymbol).s));
			}
			case Primitives.Names.Mod:
				return NumericBinop(Mod,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Min:
				return NumericMultiop(Min,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Max:
				return NumericMultiop(Max,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Abs:
				return NumericUnary(Abs,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Ceil:
				return NumericUnary(Ceil,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Floor:
				return NumericUnary(Floor,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Apply:
				if (args.Count != 2) throw new EvalException("apply requires two arguments");

				LispValue arg1result = Eval (args[1], env);
				if (!(arg1result is LispList)) throw new EvalException("the second argument to apply must be a list");

				// turn (apply func (arg1 arg2)) into (func arg1 arg2)
				{
					LispList newList = new LispList();
					newList.list.Add (Eval(args[0],env));
					newList.list.AddRange ((arg1result as LispList).list);
					return Eval (newList, env);
				}
			case Primitives.Names.ID:
				if (args.Count != 1) throw new EvalException("id requires one argument");
				return Eval(args[0],env);
			case Primitives.Names.Sqrt:
				return NumericUnary(Sqrt,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Let:
				{
					// let is just syntactic sugar
					/* The general form of a let expression is
					(let ((<var1> <exp1>)
					      (<var2> <exp2>)
					      (<varn> <expn>))
					   <body>)

					the let expression is interpreted as an alternate syntax for

					((lambda (<var1> ...<varn>)
					    <body>)
					 <exp1>
					 <expn>)*/

					if (args.Count != 2) throw new EvalException("let requires two arguments");
					if (args[0] is LispList)
					{
						LispList pairs = args[0] as LispList;
						List<LispSymbol> symbols = new List<LispSymbol>();
						List<LispValue> expressions = new List<LispValue>();
						foreach (LispValue pair in pairs.list)
						{
							if (pair is LispList && (pair as LispList).list.Count == 2 && (pair as LispList).list[0] is LispSymbol)
							{
								symbols.Add(((pair as LispList).list[0]) as LispSymbol);
								expressions.Add((pair as LispList).list[1]);
							}
							else
							{
								throw new EvalException("each element in let's first argument must be a symbol/expression pair");
							}
						}

						LispList newExpr = new LispList();
						LispList newLambda = new LispList();
						newLambda.list.Add(new LispAtom(Primitives.Names.Lambda));
						LispList lambdaVars = new LispList();
						foreach (LispSymbol symbol in symbols)
						{
							lambdaVars.list.Add(symbol);
						}
						newLambda.list.Add(lambdaVars);
						newLambda.list.Add(args[1]); // body
						newExpr.list.Add(newLambda);
						foreach (LispValue expr in expressions)
						{
							newExpr.list.Add(expr);
						}
						return Eval(newExpr, env);
					}
					else
					{
						throw new EvalException("let's first argument must be a list of symbol/expression pairs");
					}
				}
			case Primitives.Names.Pow:
				return NumericBinop(Math.Pow,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Ln:
				return NumericUnary(Math.Log,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Sin:
				return NumericUnary(Math.Sin,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Cos:
				return NumericUnary(Math.Cos,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Tan:
				return NumericUnary(Math.Tan,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Asin:
				return NumericUnary(Math.Asin,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Acos:
				return NumericUnary(Math.Acos,args,env,Primitives.names[a.primitiveIndex]);
			case Primitives.Names.Atan2:
				return NumericBinop(Math.Atan2,args,env,Primitives.names[a.primitiveIndex]);
			default:
				throw new EvalException("unimplemented primitive: " + Primitives.names[a.primitiveIndex]);
			}

			return x;
		}

		public LispValue Eval(LispValue x)
		{
			return Eval (x, globalEnv);
		}

		public LispValue Eval(LispValue x, Environment env)
		{
			if (x is LispSymbol)
			{
				LispSymbol s = x as LispSymbol;
				if (s == null) throw new EvalException("unable to cast to symbol");

				// look it up in the environment
				LispValue result = null;
				if (env.TryGetValue (s.s, out result))
				{
					return result;
				}
				else
				{
					throw new EvalException("unknown symbol: " + s.s);
				}
			}
			else if (x is LispNumber || x is LispBool || x is LispFunc || x is ExternalFunc || x is LispAtom)
			{
				return x;
			}
			else if (x is LispList)
			{
				LispList l = x as LispList;
				if (l == null) throw new EvalException("unable to cast to list");
				if (l.list.Count == 0)
					return x;

				// see if the first item is an atom
				if (l.list[0] is LispAtom)
				{
					// run the built-in functions
					return EvalAtom(x, l.list[0] as LispAtom, l.list, env);
				}
				else
				{
					// evaluate each subexpression
					List<LispValue> subexp = new List<LispValue>();
					foreach (LispValue exp in l.list)
					{
						subexp.Add (Eval (exp, env));
					}

					// at this point we must be evaluating a function
					// so the first element better be a LispFunc
					if (subexp[0] is LispFunc)
					{
						LispFunc f = subexp[0] as LispFunc;
						if (f == null) throw new EvalException("unable to cast to function");
						subexp.RemoveAt (0);
						if (f.variables.Count != subexp.Count)
							throw new EvalException("function applied to too " + (f.variables.Count < subexp.Count ? "many" : "few") + " arguments, expected: " + f.ToString());

						// apply user defined function
						return Eval(f.expression, new Environment(f.variables, subexp, f.env));
					}
					else if (subexp[0] is LispAtom)
					{
						return EvalAtom (x, subexp[0] as LispAtom, l.list, env);
					}
					else if (subexp[0] is ExternalFunc)
					{
						ExternalFunc f = subexp[0] as ExternalFunc;
						if (f == null) throw new EvalException("unable to cast to external function");
						subexp.RemoveAt (0);

						// apply externally defined function
						return f.func(subexp);
					}
					else
					{
						throw new EvalException("unable to apply non-function: " + subexp[0].ToString ());
					}
				}
			}
			else
			{
				// unexpected base class
				throw new EvalException("Unexpected: " + x.ToString ());
			}
			return x;
		}

		public string ReadEvalPrint(string line)
		{
			ParseResult parsed = Parse(line);
			if (parsed.error == "")
			{
				try
				{
					LispValue result = Eval (parsed.value, globalEnv);
					return "parsed: " + parsed.value.ToString () + "\nresult: " + result.ToString ();
				}
				catch (EvalException e)
				{
					return "error: " + e.Message;
				}
			}
			else
			{
				return "parse error: " + parsed.error;
			}
		}
	}
}

