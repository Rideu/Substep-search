using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

//using static BenchLabs.Helper;

namespace SubsetSearch
{
    class Program
    {
        #region Utils

        static void WriteLine(string s)
        {
            Debug.WriteLine(s);
            Console.WriteLine(s);
        }

        static readonly string keyspace = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
        public static string RandStringSeed(int len = 64, int seed = 1024)
        {
            var stringDerand = new Random(seed);
            var buf = "";
            for (int i = len - 1; i >= 0; i--)
            {
                buf += keyspace[stringDerand.Next(0, keyspace.Length)];
            }
            return buf;
        }

        public static byte[] SplashNXL256NO(string input, byte key)
        {
            string buf = "";
            int enh = 5345213;

            for (int i = 0; i < input.Length; i++)
            {
                enh += input[i] * enh ^ key;
            }

            for (int i = 0; i < input.Length; i++)
            {
                var c = (char)splashNXL(input[i], enh);
                buf += c + " ";
            }

            return Encoding.UTF8.GetBytes(buf);
        }

        public static int splashNXL(int x, int l) => 4 * Math.Abs(
             ash(x, l, 255) % 13
             + ash(x, l, 128) % 27
             - ash(x, l, 64) % 51
             );

        public static int ash(int x, int l, byte b) => x + NX(x + l * b);

        public static int NX(int x) => 3 + x % 2 - x % 4 + x % 8 - x % 16;

        #endregion

        // Bruteforce (https://stackoverflow.com/a/55150810)
        public static int FindLast(byte[] haystack, byte[] needle) // start <= end
        {
            for (var i = haystack.Length - 1; i >= needle.Length - 1; i--)
            {
                var found = true;

                for (var j = needle.Length - 1; j >= 0 && found; j--)
                {

                    found = haystack[i - (needle.Length - 1 - j)] == needle[j];
                }
                if (found)
                    return i - (needle.Length - 1);
            }
            return -1;
        }

        // Subset stepping
        // Worst case = Bruteforce's 
        // Best case = Bruteforce's + 1
        public static int Substep(byte[] stack, byte[] find) // start => end
        {
            for (int i = 0; i < stack.Length; i += find.Length) // o(n)
            {
                for (int fold = 0; fold < find.Length; fold++) // o(n)
                {
                    int fidx = i - fold;

                    if (find[fold] == stack[i] && fidx >= 0 && fidx + find.Length <= stack.Length)
                    {

                        if (stack[fidx] == find[0])
                        {
                            int ifind = 0;
                            for (int istack = fidx; istack < fidx + find.Length; istack++) // o(n)
                            {

                                if (stack[istack] == find[ifind])
                                {
                                    if (ifind == find.Length - 1)
                                        return fidx;
                                }
                                else
                                {
                                    break;
                                }
                                ifind++;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        class BenchPass
        {
            Action algo;

            public BenchPass(Action algo)
            {
                this.algo = algo;
            }

            public long CumulativeMillis { get; private set; }
            public long CumulativeTicks { get; private set; }

            public double MeanMillis { get; private set; }
            public double MeanTicks { get; private set; }

            public void Pass(int passes = 32, int samples = 32)
            {
                CumulativeMillis = 0;
                CumulativeTicks = 0;

                for (int pass = 0; pass < passes; pass++)
                {


                    var sw = Stopwatch.StartNew();

                    for (int i = 0; i < samples; i++)
                    {
                        algo();
                    }

                    sw.Stop();

                    //WriteLine($"[{pass}] Elapsed: {sw.ElapsedMilliseconds} ms ({sw.ElapsedTicks} t)");

                    CumulativeMillis += sw.ElapsedMilliseconds;
                    CumulativeTicks += sw.ElapsedTicks;
                }

                MeanMillis = CumulativeMillis /= passes;
                MeanTicks = CumulativeTicks /= passes;
            }
        }

        static void Main(string[] args)
        {
            var randstr = RandStringSeed(1024 * 5);
            var noise = SplashNXL256NO(randstr, 128);

            int size = 64;

            var findStart = noise.Take(size).ToArray();
            var findMiddle = noise.Skip(noise.Length / 2 - size / 2).Take(size).ToArray();
            var findEnd = noise.Skip(noise.Length - size).ToArray();

            var find = findEnd.ToArray();

            Action AlgoA = () =>
            {
                var last = FindLast(noise, findStart);
            };

            Action AlgoB = () =>
            {
                var last = Substep(noise, findEnd);

            };

            WriteLine("\n[Origin search START]\n");

            BenchPass algoA = new BenchPass(AlgoA);
            algoA.Pass(32, 512);
            WriteLine($"\n[Overall A] Mids: {algoA.MeanMillis} ms ({algoA.MeanTicks} t)\n");

            BenchPass algoB = new BenchPass(AlgoB);
            algoB.Pass(32, 512);
            WriteLine($"\n[Overall B] Mids: {algoB.MeanMillis} ms ({algoB.MeanTicks} t)\n");

            WriteLine("\n[Origin search END]");
            WriteLine($"[Results] Rel: {(int)((1 - algoB.MeanMillis / algoA.MeanMillis) * 100)}% ms ({(int)((1 - algoB.MeanTicks / algoA.MeanTicks) * 100)}% t)\n");

            AlgoA = () =>
            {
                var last = FindLast(noise, findMiddle);
            };

            AlgoB = () =>
            {
                var last = Substep(noise, findMiddle);

            };

            WriteLine("\n[Middle search START]\n");

            algoA = new BenchPass(AlgoA);
            algoA.Pass(32, 512);
            WriteLine($"\n[Overall A] Mids: {algoA.MeanMillis} ms ({algoA.MeanTicks} t)\n");

            algoB = new BenchPass(AlgoB);
            algoB.Pass(32, 512);
            WriteLine($"\n[Overall B] Mids: {algoB.MeanMillis} ms ({algoB.MeanTicks} t)\n");

            WriteLine("\n[Middle search END]");

            WriteLine($"[Results] Rel: {(int)((1 - algoB.MeanMillis / algoA.MeanMillis) * 100)}% ms ({(int)((1 - algoB.MeanTicks / algoA.MeanTicks) * 100)}% t)");

            Console.ReadKey();
        }
    }
}
