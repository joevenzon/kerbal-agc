using System;

namespace MicroLisp
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Type quit to exit");

			MicroLisp test = new MicroLisp ();

			string line = Console.ReadLine ();
			while (line != "quit") 
			{
				if (line.StartsWith("load "))
				{
					string file = line.Substring(5).Replace ("\"", "");
					try
					{
						string contents = System.IO.File.ReadAllText(file);

						Console.WriteLine ("> " + test.ReadEvalPrint(contents));
					}
					catch (Exception e)
					{
						Console.WriteLine("> unable to open file: " + e.Message);
					}
				}
				else if (line == "printenv")
				{
					Console.WriteLine(test.globalEnv.ToString());
				}
				else if (line.StartsWith("saveenv "))
				{
					try
					{
						string file = line.Substring(8).Replace ("\"", "");
						System.IO.File.WriteAllText(file, test.globalEnv.ToString().Replace("\n","\r\n"));
					}
					catch (Exception e)
					{
						Console.WriteLine("> unable to save file: " + e.Message);
					}
				}
				else if (line.StartsWith("loadenv "))
				{
					try
					{
						string file = line.Substring(8).Replace ("\"", "");
						string contents = System.IO.File.ReadAllText(file);
						test.globalEnv = new Environment();
						test.globalEnv.FromString(contents);
					}
					catch (Exception e)
					{
						Console.WriteLine("> unable to open file: " + e.Message);
					}
				}
				else
				{
					Console.WriteLine ("> " + test.ReadEvalPrint (line));
				}
				line = Console.ReadLine ();
			}
		}
	}
}
