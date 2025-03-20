using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Numerics;
using System.Security.Cryptography;
using ExtensionMethod; 

class RandomBigIntegerGenerator
    {
        public BigInteger GenerateRandomBigInteger(int bits)
        {
            int numberOfBytes = bits / 8;
            byte[] randomBytes = new byte[numberOfBytes];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return BigInteger.Abs(new BigInteger(randomBytes));
        }
    }

class FactorCounter
    {
        public int CountFactors(BigInteger number)
        {
            int factorCount = 0;
            int sqrt = (int)Math.Ceiling(Math.Sqrt((double)number));

            Parallel.For(1, sqrt, i =>
            {
                if (number % i == 0)
                {
                    Interlocked.Add(ref factorCount, 2);
                }
            });

            if (sqrt * sqrt == number)
            {
                factorCount++;
            }

            return factorCount;
        }
    }


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Environment.ProcessorCount);
        
        if (args.Length < 2 || args.Length > 3)
        {
            Console.WriteLine("Usage: dotnet run <bits> <option> <count>");
            return;
        }

        if (!int.TryParse(args[0], out int bits) || bits < 32 || bits % 8 != 0)
        {
            Console.WriteLine("Error: bits must be a multiple of 8 and at least 32.");
            return;
        }

        string option = args[1].ToLower();

        int count = args.Length == 3 && int.TryParse(args[2], out int parsedCount) ? parsedCount : 1;

        Stopwatch stopwatch = Stopwatch.StartNew();

        RandomBigIntegerGenerator generator = new RandomBigIntegerGenerator();

        Console.WriteLine($"Bitlength: {bits} bits");
        
        for (int i = 0; i < count; i++)
        {
            BigInteger randomNumber = generator.GenerateRandomBigInteger(bits);

            if (option == "prime")
            {
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount*2 };

                ConcurrentBag<BigInteger> primeNumbers = new ConcurrentBag<BigInteger>();
                int counter = 0;

                Parallel.For(1, int.MaxValue, parallelOptions, (i, pls) =>
                {
                    BigInteger primeCandidate = generator.GenerateRandomBigInteger(bits);
                    counter++;
                    if (primeCandidate.IsProbablyPrime())
                    {
                        primeNumbers.Add(primeCandidate);
                    }
                    if(primeNumbers.Count == count)
                    {
                        pls.Stop();
                    }
                    if (counter % 1000 == 0)
                    {
                        Console.WriteLine($"Checked {counter} numbers.");
                    }
                });

                Console.WriteLine($"Prime Numbers Found: {string.Join(", ", primeNumbers)}");

                Console.WriteLine($"{i + 1}: {randomNumber}");
                Console.WriteLine($"Number of attempts to find prime: {counter}");
                Console.WriteLine($"Time Per Iteration: {stopwatch.Elapsed.Milliseconds/(counter*1.0)}ms");
            }
            else if (option == "odd")
            {
                FactorCounter factorCounter = new FactorCounter();

                while (randomNumber % 2 == 0)
                {
                    randomNumber = generator.GenerateRandomBigInteger(bits);
                }

                Console.WriteLine($"{i + 1}: {randomNumber}");
                int factorCount = factorCounter.CountFactors(randomNumber);
                Console.WriteLine($"Number of factors: {factorCount}\n");
            }
            else
            {
                Console.WriteLine("Error: option must be 'prime' or 'odd'.");
                return;
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"Time to Generate: {stopwatch.Elapsed}");
    }
}


namespace ExtensionMethod
{
    public static class PrimeChecker
    {
        public static bool IsProbablyPrime(this BigInteger value, int k = 10)
        {
            if (value < 2)
                return false;
            if (value == 2 || value == 3)
                return true;
            if (value % 2 == 0)
                return false;

            BigInteger d = value - 1;
            int s = 0;
            while (d % 2 == 0)
            {
                d /= 2;
                s++;
            }

            for (int i = 0; i < k; i++)
            {
                BigInteger a = GenerateRandomBase(value);
                BigInteger x = BigInteger.ModPow(a, d, value); 

                if (x == 1 || x == value - 1)
                    continue; 

                bool composite = true;
                for (int j = 0; j < s - 1; j++)
                {
                    x = BigInteger.ModPow(x, 2, value); 

                    if (x == 1)
                        return false; 
                    if (x == value - 1)
                    {
                        composite = false;
                        break; 
                    }
                }

                if (composite)
                    return false; 
            }

            return true; 
        }

        private static BigInteger GenerateRandomBase(BigInteger value)
        {
            Random random = new Random();
            byte[] bytes = new byte[value.ToByteArray().Length];
            random.NextBytes(bytes);
            return new BigInteger(bytes) % (value - 2) + 2;
        }
    }
}



