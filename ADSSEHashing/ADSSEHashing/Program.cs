using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ADSSEHashing
{
    enum ConflictSolver {OPEN_ADDRESS_LINEAR, OPEN_ADDRESS_QUADRATIC, OPEN_ADDRESS_HASH2, CHAINING}
    enum HashMethod { DIVISION, MULTIPLICATION, MID_SQUARE}
    class Program
    {
        #region Classes
        class Hashable
        {
            private int _key;
            public int key { get { return _key; } set { _key = value; } }

            private string _value;
            public string value { get { return _value; } set { _value = value; } }

            private int _hash;
            public int hash { get { return _hash; } set { _hash = value; } }


            public Hashable(string value)
            {
                _value = value;
                _key = GenerateKey(_value);
            }

            private int GenerateKey(string value)
            {
                int key = 0;
                byte[] asciiBytes = Encoding.ASCII.GetBytes(value);

                for(int i = 0; i < asciiBytes.Length; i++)
                {
                    key += asciiBytes[i];
                }

                return key;
            }
        }

        class Hashtable
        {
            private int _m;
            public int m { get { return _m; } set { _m = m; } }

            private ConflictSolver _conflictSolver;
            public ConflictSolver conflictSolver { get { return _conflictSolver; } set { _conflictSolver = conflictSolver; } }

            private HashMethod _hashMethod;
            public HashMethod hashMethod { get { return _hashMethod; } set { _hashMethod = hashMethod; } }

            private Hashable[] _hTable;
            private LinkedList<Hashable>[] _lTable;

            private float A = MathF.Sqrt(2) - 1;

            public Hashtable(int m, ConflictSolver conflictSolver, HashMethod hashMethod)
            {
                _conflictSolver = conflictSolver;
                _hashMethod = hashMethod;
                
                switch (hashMethod)
                {
                    case HashMethod.MULTIPLICATION:
                        if (!IsPow2(m))
                        {
                            _m = ToNextNearestPow2(m);
                            Console.WriteLine("m has been set to " + _m + " as its the next power of 2");
                            break;
                        }
                        _m = m;
                        break;

                    case HashMethod.DIVISION:
                        if (IsPow2(m))
                        {
                            int next = ToNextNearestPow2(m);
                            _m = (m - next) + m;
                            Console.WriteLine("m is " + _m + " as defined m was a power of 2");
                            break;
                        }
                        _m = m;
                        break;

                    case HashMethod.MID_SQUARE:
                        if (m > 999)
                        {
                            _m = m;
                            break;
                        }
                        if(m > 99)
                        {
                            _m = 1000;
                            break;
                        }
                        _m = 100;
                        break;

                    default:
                        break;
                }

                if(conflictSolver == ConflictSolver.CHAINING)
                {
                    _lTable = new LinkedList<Hashable>[_m];
                    for(int i = 0; i < _lTable.Length; i++)
                    {
                        _lTable[i] = new LinkedList<Hashable>();
                    }
                }

                else if (conflictSolver == ConflictSolver.OPEN_ADDRESS_LINEAR || conflictSolver == ConflictSolver.OPEN_ADDRESS_QUADRATIC || conflictSolver == ConflictSolver.OPEN_ADDRESS_HASH2)
                    _hTable = new Hashable[_m];
            }

            public float getLoadFactor(int n, LinkedList<Hashable>[] arr)
            {
                if(arr != null && arr.Length > 0)
                {
                    return n / arr.Length;
                }
                return 0f;
            }

            public float getLoadFactor(int n, Hashable[] arr)
            {
                if (arr != null && arr.Length > 0)
                {
                    return n / arr.Length;
                }
                return 0f;
            }

            public bool Delete(string value)
            {
                Hashable hashable = new Hashable(value);
                int hash = -1;
                switch (_hashMethod)
                {
                    case HashMethod.DIVISION:
                        hash = HashFunctions.DivisionHash(hashable, _m);
                        break;

                    case HashMethod.MULTIPLICATION:
                        hash = HashFunctions.MultiplicationHash(hashable, _m, A);
                        break;

                    case HashMethod.MID_SQUARE:
                        hash = HashFunctions.MidSquareHash(hashable, _m);
                        break;

                    default:
                        break;
                }
                switch (conflictSolver)
                {
                    case ConflictSolver.CHAINING:
                        if (hash > -1)
                        {
                            if (_lTable[hash] != null)
                            {
                                LinkedListNode<Hashable> node = _lTable[hash].First;
                                if (node.Value.value.Equals(hashable.value))
                                {
                                    _lTable[hash].Remove(node);
                                    return true;
                                }
                                for (int i = 1; i < _lTable[hash].Count; i++)
                                {
                                    node = node.Next;
                                    if (node.Value.value.Equals(hashable.value))
                                    {
                                        _lTable[hash].Remove(node);
                                        return true;
                                    }
                                }
                            }
                        }
                        return false;

                    case ConflictSolver.OPEN_ADDRESS_LINEAR:
                        if (hash > -1)
                        {
                            for (int i = hash; i < _hTable.Length; i++)
                            {
                                if (_hTable[i] != null)
                                {
                                    if (_hTable[i].value.Equals(hashable.value))
                                    {
                                        _hTable[i] = null;
                                        return true;
                                    }

                                }
                                if (i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return false;
                    case ConflictSolver.OPEN_ADDRESS_QUADRATIC:
                        if (hash > -1)
                        {
                            if (_hTable[hash] != null)
                            {
                                if (_hTable[hash].value.Equals(hashable.value))
                                {
                                    _hTable[hash] = null;
                                    return true;
                                }
                            }

                            for (long i = 0; i < _hTable.Length; i++)
                            {
                                long k = (hash + i * i) % _hTable.Length;
                                if (_hTable[k] != null)
                                {
                                    if (_hTable[k].value.Equals(hashable.value))
                                    {
                                        _hTable[i] = null;
                                        return true;
                                    }
                                }
                                if (i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return false;


                    case ConflictSolver.OPEN_ADDRESS_HASH2:
                        if (hash > -1)
                        {
                            if (_hTable[hash] != null)
                            {
                                if (_hTable[hash].value.Equals(hashable.value))
                                {
                                    _hTable[hash] = null;
                                    return true;
                                }
                            }

                            for (int i = 0; i < _hTable.Length; i++)
                            {
                                int k = (hash + i * HashFunctions.DivisionHash(hashable, _m)) % _hTable.Length;
                                if (_hTable[k] != null)
                                {
                                    if (_hTable[k].value.Equals(hashable.value))
                                    {
                                        _hTable[k] = null;
                                        return true;
                                    }
                                }
                                if (i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return false;

                }

                return false;
            }

            public Hashable Search(string value)
            {
                Hashable hashable = new Hashable(value);
                int hash = -1;
                switch (_hashMethod)
                {
                    case HashMethod.DIVISION:
                        hash = HashFunctions.DivisionHash(hashable, _m);
                        break;

                    case HashMethod.MULTIPLICATION:
                        hash = HashFunctions.MultiplicationHash(hashable, _m, A);
                        break;

                    case HashMethod.MID_SQUARE:
                        hash = HashFunctions.MidSquareHash(hashable, _m);
                        break;

                    default:
                        break;
                }
                switch (conflictSolver)
                {
                    case ConflictSolver.CHAINING:
                        if (hash > -1)
                        {
                            if (_lTable[hash] != null)
                            {
                                LinkedListNode<Hashable> node = _lTable[hash].First;
                                if (node.Value.value.Equals(hashable.value))
                                {
                                    return node.Value;
                                }
                                for(int i = 1; i < _lTable[hash].Count; i++)
                                {
                                    node = node.Next;
                                    if (node.Value.value.Equals(hashable.value))
                                    {
                                        return node.Value;
                                    }
                                }
                            }
                        }
                        return null;

                    case ConflictSolver.OPEN_ADDRESS_LINEAR:
                        if (hash > -1)
                        {
                            for (int i = hash; i < _hTable.Length; i++)
                            {
                                if (_hTable[i] != null)
                                {
                                    if (_hTable[i].value.Equals(hashable.value))
                                    {
                                        return hashable;
                                    }
                                   
                                }
                                if(i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return null;
                    case ConflictSolver.OPEN_ADDRESS_QUADRATIC:
                        if (hash > -1)
                        {
                            if (_hTable[hash] != null)
                            {
                                if (_hTable[hash].value.Equals(hashable.value))
                                {
                                    return hashable;
                                }
                            }

                            for (long i = 0; i < _hTable.Length; i++)
                            {
                                long k = (hash + i * i) % _hTable.Length;
                                if (_hTable[k] != null)
                                {
                                    if (_hTable[k].value.Equals(hashable.value))
                                    {
                                        return hashable;
                                    }
                                }
                                if (i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return null;
                        

                    case ConflictSolver.OPEN_ADDRESS_HASH2:
                        if (hash > -1)
                        {
                            if (_hTable[hash] != null)
                            {
                                if (_hTable[hash].value.Equals(hashable.value))
                                {
                                    return hashable;
                                }
                            }

                            for (int i = 0; i < _hTable.Length; i++)
                            {
                                int k = (hash + i * HashFunctions.DivisionHash(hashable, _m)) % _hTable.Length;
                                if (_hTable[k] != null)
                                {
                                    if (_hTable[k].value.Equals(hashable.value))
                                    {
                                        return hashable;
                                    }
                                }
                                if (i == _hTable.Length - 1)
                                {
                                    i = -1;
                                }
                            }
                        }
                        return null;
                        
                }

                return null;
            }


            public bool Insert(Hashable x)
            {
                int hash = -1;
                switch (_hashMethod)
                {
                    case HashMethod.DIVISION:
                        hash = HashFunctions.DivisionHash(x, _m);
                        break;

                    case HashMethod.MULTIPLICATION:
                        hash = HashFunctions.MultiplicationHash(x, _m, A);
                        break;

                    case HashMethod.MID_SQUARE:
                        hash = HashFunctions.MidSquareHash(x, _m);
                        break;

                    default:
                        break;
                }

                switch (conflictSolver)
                {
                    case ConflictSolver.CHAINING:
                        if(hash > -1)
                        {
                            if (_lTable[hash] != null)
                            {
                                _lTable[hash].AddFirst(x);
                                return true;
                            }

                            LinkedList<Hashable> list = new LinkedList<Hashable>();
                            list.AddFirst(x);
                            _lTable[hash] = list;
                            return true;
                        }
                        return false;

                    case ConflictSolver.OPEN_ADDRESS_LINEAR:
                        if(hash > -1)
                        {
                            for (int i = hash; i < _hTable.Length; i++)
                            {
                                if (_hTable[i] == null)
                                {
                                    _hTable[i] = x;
                                    return true;
                                }
                            }
                        }
                        return false;
                    case ConflictSolver.OPEN_ADDRESS_QUADRATIC:
                        if(hash > -1)
                        {
                            if(_hTable[hash] == null)
                            {
                                _hTable[hash] = x;
                                return true;
                            }
                            for (long i = 0; i < _hTable.Length; i++)
                            {
                                long k = (hash + i * i) % _hTable.Length;
                                if (_hTable[k] == null)
                                {
                                    _hTable[k] = x;
                                    return true;
                                }
                            }
                        }
                        return false;

                    case ConflictSolver.OPEN_ADDRESS_HASH2:
                        if(hash > -1)
                        {
                            if (_hTable[hash] == null)
                            {
                                _hTable[hash] = x;
                                return true;
                            }

                            for(int i = 0; i < _hTable.Length; i++)
                            {
                                int k = (hash + i * HashFunctions.DivisionHash(x, _m)) % _hTable.Length;
                                if (_hTable[k] == null)
                                {
                                    _hTable[k] = x;
                                    return true;
                                }
                            }
                        }
                        return false;
                }
                return false;
            }

            public void printTable()
            {
                switch (conflictSolver)
                {
                    case ConflictSolver.CHAINING:
                        foreach(LinkedList<Hashable> key in _lTable)
                        {
                            LinkedListNode<Hashable> node = key.First;
                            if (node != null)
                            {
                                Console.WriteLine("Hash: " + node.Value.hash + " | Key: " + node.Value.key + " | Value: " + node.Value.value);
                                while (node.Next != null)
                                {
                                    node = node.Next;
                                    Console.WriteLine("Hash: " + node.Value.hash + " | Key: " + node.Value.key + " | Value: " + node.Value.value);
                                }
                            }
                        }
                        break;

                    default:
                        foreach (Hashable key in _hTable)
                        {
                            if(key != null)
                            {
                                Console.WriteLine("Hash: " + key.hash + " | Key: " + key.key + " | Value: " + key.value);
                            }
                        }
                        break;
                }
            }

            #region Find Power of 2
            int ToNextNearestPow2(int x)
            {
                if (x < 0) { return 0; }
                --x;
                x |= x >> 1;
                x |= x >> 2;
                x |= x >> 4;
                x |= x >> 8;
                x |= x >> 16;
                return x + 1;
            }

            bool IsPow2(int x)
            {
                return (x & (x - 1)) == 0;
            }

            #endregion

        }

        static class HashFunctions
        {
            public static int DivisionHash(Hashable x, int m)
            {
                int hash = x.key % m;
                x.hash = hash;
                return hash;
            }

            public static int MultiplicationHash(Hashable x, int m, float A)
            {
                int hash = (int)MathF.Floor(m * (x.key * A % 1));
                x.hash = hash;
                return hash;
            }

            public static int MidSquareHash(Hashable x, int m)
            {
                long k = x.key * x.key;
                int hash;

                if(m > 100)
                {
                    hash = getMiddle3(k);
                    x.hash = hash;
                    return hash;
                }

                hash = getMiddle2(k);
                x.hash = hash;
                return hash;
            }

            private static int getMiddle2(long num)
            {
                string s = num.ToString();
                int len = s.Length;
                int midNum = (int)MathF.Floor(len / 2);
                string mid = s.Substring(midNum, 2);
                return int.Parse(mid);

            }

            private static int getMiddle3(long num)
            {
                string s = num.ToString();
                int len = s.Length;
                int midNum = (int)MathF.Floor(len / 2);
                string mid = s.Substring(midNum, 3);
                return int.Parse(mid);

            }

        }
        #endregion


        private static string[] txtToArr(string path)
        {
            return File.ReadAllLines(path);
        }

        private static string[] GenerateNameArr(string[] firstnames, string[] lastnames, int len)
        {
            Random rand = new Random();
            string[] returnArr = new string[len];

            for(int i = 0; i < returnArr.Length; i++)
            {
                returnArr[i] = firstnames[rand.Next(firstnames.Length)] + " " + lastnames[rand.Next(lastnames.Length)];
            }

            return returnArr;
        }

        static int read;
        static string linebreak = "\n_______________________________________________________________________\n";
        static HashMethod hashMethod;
        static ConflictSolver conflictSolver;
        static string[] firstnames = txtToArr("firstnames.txt");
        static string[] lastnames = txtToArr("lastnames.txt");
        static string[] names;
        static int len;
        static Random random = new Random();
        static Stopwatch stopwatch = new Stopwatch();
        static string randName;
        static Hashable result;
        static bool val;
        static void Main(string[] args)
        {
            while (true)
            {
                if (names == null)
                {
                    Console.WriteLine("Step 1: Generate list of names for hashing. \n Please enter desired amount of names as an integer: ");
                    len = int.Parse(Console.ReadLine());
                    Console.WriteLine("Generating list of names...");

                    names = GenerateNameArr(firstnames, lastnames, len);
                }


                Console.WriteLine("Options: \n" +
                    "0| Compare Hashing Functions (Compares running time of Hashing Functions for the generated list of names, using the same probing method)\n" +
                    "1| Compare Probing Methods (Compares running time of probing methods using a chosen hashing function on the generated list of names)\n" +
                    "2| Run Customized Hashing (Choose a Hashing Function and a Probing Method to run for evaluation)");
                int input = int.Parse(Console.ReadLine());

                switch (input)
                {
                    case 0:
                        Console.WriteLine(linebreak);
                        Console.WriteLine("Please choose a conflict resolution method: \n" +
                            "0| Open Addressing (Linear Probing) \n" +
                            "1| Open Addressing (Quadratic Probing) \n" +
                            "2| Open Addressing (Double Hashing)\n" +
                            "3| Chaining");

                        read = int.Parse(Console.ReadLine());
                        if (read < 2 && read >= 0)
                        {
                            conflictSolver = (ConflictSolver)read;
                        }
                        else conflictSolver = ConflictSolver.CHAINING;

                        Hashtable divTable = new Hashtable(len, conflictSolver, HashMethod.DIVISION);
                        Hashtable multipTable = new Hashtable(len, conflictSolver, HashMethod.MULTIPLICATION);
                        Hashtable midsqTable = new Hashtable(len, conflictSolver, HashMethod.MID_SQUARE);


                        Console.WriteLine("Populating Division Hashing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            divTable.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long dvTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("Populating Multiplication Hashing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            multipTable.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long multTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("Populating Mid Squared Hashing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            midsqTable.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long msqTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + dvTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + multTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + msqTime + " Milliseconds\n");

                        Console.WriteLine("Fetching random name for search");
                        randName = names[random.Next(names.Length - 1)];
                        Console.WriteLine("Searching for: " + randName);

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = divTable.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Division Hashing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        dvTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = multipTable.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Multiplication Hashing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        multTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = midsqTable.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Mid-Squared Hashing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        msqTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + dvTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + multTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + msqTime + " Milliseconds \n");


                        Console.WriteLine("Fetching random name for delete ");
                        randName = names[random.Next(names.Length - 1)];
                        Console.WriteLine("Searching for: " + randName);

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = divTable.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Division Hashing| Delete Successful?:" + val);

                        dvTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = multipTable.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Multiplication Hashing| Delete Successful?:" + val);

                        multTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = midsqTable.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Mid-Squared Hashing| Delete Successful?:" + val);

                        msqTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + dvTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + multTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + msqTime + " Milliseconds ");
                        break;
                    case 1:
                        Console.WriteLine(linebreak);
                        Console.WriteLine("Please choose a hashing function: \n" +
                            "0| Division Hashing \n" +
                            "1| Multiplication Hashing \n" +
                            "2| Mid-Squared Hashing");

                        read = int.Parse(Console.ReadLine());
                        if (read < 3 && read >= 0)
                        {
                            hashMethod = (HashMethod)read;
                        }
                        else hashMethod = HashMethod.DIVISION;

                        Hashtable linearProbing = new Hashtable(len, ConflictSolver.OPEN_ADDRESS_LINEAR, hashMethod);
                        Hashtable quadraticProbing = new Hashtable(len, ConflictSolver.OPEN_ADDRESS_QUADRATIC, hashMethod);
                        Hashtable doubleHashProbing = new Hashtable(len, ConflictSolver.OPEN_ADDRESS_HASH2, hashMethod);
                        Hashtable chainProbing = new Hashtable(len, ConflictSolver.CHAINING, hashMethod);

                        Console.WriteLine("Populating Linear Probing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            linearProbing.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long lpTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("Populating Quadratic Probing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            quadraticProbing.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long qdTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("Populating Double Hasing Probing Table: ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            doubleHashProbing.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long dhTime = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine("Populating Chaining Probing Table: \n ");

                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            chainProbing.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        long cnTime = stopwatch.ElapsedMilliseconds;


                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + lpTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + qdTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + dhTime + " Milliseconds ");
                        Console.WriteLine("Chaining Probing |Time Elapsed: " + cnTime + " Milliseconds \n");

                        Console.WriteLine("Fetching random name for search");
                        randName = names[random.Next(names.Length - 1)];
                        Console.WriteLine("Searching for: " + randName);

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = linearProbing.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Linear Probing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        lpTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = quadraticProbing.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Quadratic Probing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        qdTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = doubleHashProbing.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Double Hash Probing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        dhTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        result = chainProbing.Search(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Chaining Probing| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);

                        cnTime = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + lpTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + qdTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + dhTime + " Milliseconds ");
                        Console.WriteLine("Chaining Probing |Time Elapsed: " + cnTime + " Milliseconds \n");


                        Console.WriteLine("Fetching random name for delete ");
                        randName = names[random.Next(names.Length - 1)];
                        Console.WriteLine("Searching for: " + randName);

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = linearProbing.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Linear Probing| Delete Successful?:" + val);

                        lpTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = quadraticProbing.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Quadratic Probing| Delete Successful?:" + val);

                        qdTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = doubleHashProbing.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Double Hash Probing| Delete Successful?:" + val);

                        dhTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Reset();
                        stopwatch.Start();
                        val = chainProbing.Delete(randName);
                        stopwatch.Stop();
                        Console.WriteLine("Chaining Probing| Delete Successful?:" + val);

                        cnTime = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine("\nLinear Probing |Time Elapsed: " + lpTime + " Milliseconds ");
                        Console.WriteLine("Quadratic Probing |Time Elapsed: " + qdTime + " Milliseconds ");
                        Console.WriteLine("Double Hashing Probing |Time Elapsed: " + dhTime + " Milliseconds ");
                        Console.WriteLine("Chaining Probing |Time Elapsed: " + cnTime + " Milliseconds ");
                        break;

                    case 2: //Customized Hashing Function
                        Console.WriteLine(linebreak);
                        Console.WriteLine("Please choose a hashing function: \n" +
                            "0| Division Hashing \n" +
                            "1| Multiplication Hashing \n" +
                            "2| Mid-Squared Hashing");

                        read = int.Parse(Console.ReadLine());
                        if (read < 3 && read >= 0)
                        {
                            hashMethod = (HashMethod)read;
                        }
                        else hashMethod = HashMethod.DIVISION;

                        Console.WriteLine(linebreak);
                        Console.WriteLine("Please choose a conflict resolution method: \n" +
                            "0| Open Addressing (Linear Probing) \n" +
                            "1| Open Addressing (Quadratic Probing) \n" +
                            "2| Open Addressing (Double Hashing)\n" +
                            "3| Chaining");

                        read = int.Parse(Console.ReadLine());
                        if (read < 2 && read >= 0)
                        {
                            conflictSolver = (ConflictSolver)read;
                        }
                        else conflictSolver = ConflictSolver.CHAINING;

                        Console.WriteLine(linebreak);
                        Console.WriteLine("Current Settings: ");
                        Console.WriteLine("Hashing Method: " + hashMethod);
                        Console.WriteLine("Conflict Resolution Method: " + conflictSolver);

                        Hashtable table = new Hashtable(len, conflictSolver, hashMethod);


                        Console.WriteLine("\nCreating Hashtable");
                        stopwatch.Reset();
                        stopwatch.Start();
                        foreach (string s in names)
                        {
                            table.Insert(new Hashable(s));
                        }
                        stopwatch.Stop();
                        Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds + "Milliseconds \n");

                        Console.WriteLine("Options: \n" +
                            "0| Search\n" +
                            "1| Delete");
                        read = int.Parse(Console.ReadLine());
                        if (read == 0)
                        {
                            bool flag = false;
                            while (!flag)
                            {
                                Console.WriteLine("Searching for random name in hashing table");
                                randName = names[random.Next(names.Length - 1)];
                                Console.WriteLine("Searching for: " + randName);
                                stopwatch.Reset();
                                stopwatch.Start();
                                Hashable result = table.Search(randName);
                                stopwatch.Stop();
                                Console.WriteLine("Hashable| Value: " + result.value + "| Key: " + result.key + "| Hash: " + result.hash);
                                Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds + "Milliseconds\n");
                                Console.WriteLine("Start new search? Y/N");
                                ConsoleKeyInfo r = Console.ReadKey();
                                if (r.Key == ConsoleKey.N) flag = true;
                            }
                        }
                        else if (read == 1)
                        {
                            bool flag = false;
                            while (!flag)
                            {
                                Console.WriteLine("Deleting random name in hashing table");
                                randName = names[random.Next(names.Length - 1)];
                                Hashable val = table.Search(randName);
                                Console.WriteLine("Deleting " + "Hashable| Value: " + val.value + "| Key: " + val.key + "| Hash: " + val.hash);
                                stopwatch.Reset();
                                stopwatch.Start();
                                bool result = table.Delete(randName);
                                stopwatch.Stop();
                                Console.WriteLine("Delete completed: " + result);
                                Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds + "Milliseconds\n");
                                Console.WriteLine("Start new search? Y/N");
                                ConsoleKeyInfo r = Console.ReadKey();
                                if (r.Key == ConsoleKey.N) flag = true;
                            }
                        }
                        break;

                    default:
                        Console.WriteLine("Input not valid");
                        break;

                }


                //table.printTable();


                Console.ReadLine();
            }
        }
    }
}
