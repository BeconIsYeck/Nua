using System;
using KeraLua;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;


namespace Nua {
	public class Nua {
		public Nua() { }

		static bool validArg(string arg) =>
			arg == "-y" | arg == "-n";

		public static void Main(string[] args) {
			var nua = new Nua();

			if (args.Length == 0) {
				Console.WriteLine("No arguments. Add '-?' as argument for help");
			}

			if (args.Length == 1) {
				if (args[0] == "-?") {
                    Console.WriteLine(@"//-- Nua Standard CLI Transpiler --//
	Written by David J. O-N (BeconIsYeck / Babybunnyiscute19)

Options:
	0:
		Nua.exe
			Prompts user to see options.		

		Nua.exe -?
			Opens options menu.
		
		Nua.exe <nua path> -y/-n -y/-n -y/n <lua path>
			Runs .nua file with source dump (-y/-n), _program dump (-y/-n), or return code (-y/-n)
			Will output a .lua file at lua path. (optional)

		Nua.exe -n <nua path> -l <lua path> -o <exe path>
			Generates .lua and .exe files using .nua source.
");
                }
			}

			if (args.Length == 6) {
				if (args[0] == "-n" &&
					args[2] == "-l" &&
					args[4] == "-o") {

					var target = new StreamReader(args[1]);

					var targetSrc = target.ReadToEnd();

					var luaSrc = nua.Transpile(targetSrc, false, false);

					File.WriteAllText(args[3], luaSrc);

					var nuaDirPath = Assembly.GetEntryAssembly()?.Location;

					nuaDirPath = nuaDirPath?.Remove(nuaDirPath.Length - 26);

                    var rtcArgs = " -s " + args[3] + " -o " + args[5];

                    Process.Start(nuaDirPath + "rtc\\rtc-1.6.0-x64\\rtc.exe", rtcArgs);
                }
			}

			if (args.Length >= 4) {
				if (args[0] != "-n") {
					var file = new StreamReader(args[0]);

					var src = file.ReadToEnd();

					if (validArg(args[1]) && validArg(args[2]) && validArg(args[3])) {
						var printSrcDump = args[1] == "-y";
						var print_nua_program = args[2] == "-y";
						var retCode = args[3] == "-y";

						if (args.Length == 5) {
							var luaCompPath = args[4];

							File.WriteAllText(luaCompPath, nua.Transpile(src, printSrcDump, false));
						}
						else {
							if (retCode)
								Console.WriteLine("\nReturn code: " + nua.Run("\n" + src + "\n", true, true));
							else
								nua.Run(src, printSrcDump, print_nua_program);
						}
					}
					else {
						Console.WriteLine("Invalid arguments. Add '-?' as argument for help.");
					}
				}
			}
		}

		public string BasicTranspile(string src) {
			src = " \n " + src + " \n ";

			var tokens = Transpiler.Lexer.Tokenize(src);

			var luaStatements = Transpiler.StatementAnalyzer.Analyze(tokens);

			var program = "";

			var addTab = false;

			foreach (var luaStm in luaStatements) {
				if (!addTab) {
					addTab = true;
					program += luaStm + "\n";
				}
				else {
					program += "\t" + luaStm + "\n";
				}
			}

			return program;
		}

		public string Transpile(string src, bool printDump, bool print_nua_program) {
			src = " \n " + src + " \n ";

			var prefix = @"	local _nua_program={};
	local function _nua_program_iforeach(t)
	local i=-1
	return function()
	i=i+1
	if t[i]~=nil then
	return i,t[i]
	end
	end
	end
";

			var suffix = @"	local _nua_program_i=1;
	for _nua_program_func,_ in pairs(_nua_program)do
	if _nua_program_func==""main""then
	_nua_program.main();
	_nua_program[_nua_program_func]=nil;
	end
	_nua_program_i=_nua_program_i+1;
	end
	return _nua_program;";

			var tokens = Transpiler.Lexer.Tokenize(src);
			var luaStatements = Transpiler.StatementAnalyzer.Analyze(tokens);

			var program = "";

			foreach (var luaStm in luaStatements) {
				program += "\t" + luaStm + "\n";
			}

			if (print_nua_program) {
				suffix = @"	print(""_nua_program dump:"");
	for _nua_program_i,_nua_program_v in pairs(_nua_program)do
	print(""\t"", _nua_program_i, _nua_program_v)
	end
	print()
	local _nua_program_i=1;
	for _nua_program_func,_ in pairs(_nua_program)do
	if _nua_program_func==""main""then
	_nua_program.main();
	_nua_program[_nua_program_func]=nil;
	end
	_nua_program_i=_nua_program_i+1;
	end
	return _nua_program;";
			}
			program = prefix + program;
			program += suffix;

			if (printDump)
				Console.WriteLine("src dump: \n" + program + "\n");

			return program;
		}

		public int Run(string src, bool printDump, bool print_nua_program) {
			var lua = new Lua();

			var output = lua.DoString(this.Transpile(src, printDump, print_nua_program));

			return output ? 1 : 0;
		}
	}
}
