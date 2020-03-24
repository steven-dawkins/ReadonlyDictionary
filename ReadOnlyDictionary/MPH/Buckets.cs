/* ........................................................................ *
 * (c) 2010 Laurent Dupuis (www.dupuis.me)                                  *
 * ........................................................................ *
 * < This program is free software: you can redistribute it and/or modify
 * < it under the terms of the GNU General Public License as published by
 * < the Free Software Foundation, either version 3 of the License, or
 * < (at your option) any later version.
 * <
 * < This program is distributed in the hope that it will be useful,
 * < but WITHOUT ANY WARRANTY; without even the implied warranty of
 * < MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * < GNU General Public License for more details.
 * <
 * < You should have received a copy of the GNU General Public License
 * < along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * ........................................................................ */
namespace MPHTest.MPH
{
    using System;

    internal struct BucketSortedList
    {
        public uint BucketsList;
        public uint Size;
    }

    internal class Buckets
    {
        private const uint KeysPerBucket = 4; // average number of keys per bucket
        private const uint MaxProbesBase = 1 << 20;

        private struct Item
        {
            public uint F;
            public uint H;
        }

        private struct Bucket
        {
            public uint ItemsList; // offset
            private uint _sizeBucketID;

            public uint Size
            {
                get { return this._sizeBucketID; } set { this._sizeBucketID = value; }
            }

            public uint BucketID
            {
                get { return this._sizeBucketID; } set { this._sizeBucketID = value; }
            }
        }

        private struct MapItem
        {
            public uint F;
            public uint H;
            public uint BucketNum;
        }

;

        private Bucket[] _buckets;
        private Item[] _items;
        private readonly uint _nbuckets;	    // number of buckets
        private readonly uint _n;			    // number of bins
        private readonly uint _m;				// number of keys
        private readonly IKeySource _keySource;

        public uint NBuckets
        {
            get { return this._nbuckets; }
        }

        public uint N
        {
            get { return this._n; }
        }

        public Buckets(IKeySource keySource, double c)
        {
            this._keySource = keySource;

            var loadFactor = c;
            this._m = keySource.NbKeys;
            this._nbuckets = this._m / KeysPerBucket + 1;

            if (loadFactor < 0.5)
            {
                loadFactor = 0.5;
            }

            if (loadFactor >= 0.99)
            {
                loadFactor = 0.99;
            }

            this._n = (uint)(this._m / (loadFactor)) + 1;

            if (this._n % 2 == 0)
            {
                this._n++;
            }

            for (; ;)
            {
                if (MillerRabin.CheckPrimality(this._n))
                {
                    break;
                }

                this._n += 2; // just odd numbers can be primes for n > 2
            }

            this._buckets = new Bucket[this._nbuckets];
            this._items = new Item[this._m];
        }

        private bool BucketsInsert(MapItem[] mapItems, uint itemIdx)
        {
            var bucketIdx = mapItems[itemIdx].BucketNum;
            var p = this._buckets[bucketIdx].ItemsList;

            for (uint i = 0; i < this._buckets[bucketIdx].Size; i++)
            {
                if (this._items[p].F == mapItems[itemIdx].F && this._items[p].H == mapItems[itemIdx].H)
                {
                    return false;
                }

                p++;
            }

            this._items[p].F = mapItems[itemIdx].F;
            this._items[p].H = mapItems[itemIdx].H;
            this._buckets[bucketIdx].Size++;
            return true;
        }

        private void BucketsClean()
        {
            for (uint i = 0; i < this._nbuckets; i++)
            {
                this._buckets[i].Size = 0;
            }
        }

        public bool MappingPhase(out uint hashSeed, out uint maxBucketSize)
        {
            var hl = new uint[3];
            var mapItems = new MapItem[this._m];
            uint mappingIterations = 1000;
            var rdm = new Random(111);

            maxBucketSize = 0;
            for (; ;)
            {
                mappingIterations--;
                hashSeed = (uint)rdm.Next((int)this._m); // ((cmph_uint32)rand() % this->_m);

                this.BucketsClean();

                this._keySource.Rewind();

                uint i;
                for (i = 0; i < this._m; i++)
                {
                    JenkinsHash.HashVector(hashSeed, this._keySource.Read(), hl);

                    uint g = hl[0] % this._nbuckets;
                    mapItems[i].F = hl[1] % this._n;
                    mapItems[i].H = hl[2] % (this._n - 1) + 1;
                    mapItems[i].BucketNum = g;

                    this._buckets[g].Size++;
                    if (this._buckets[g].Size > maxBucketSize)
                    {
                        maxBucketSize = this._buckets[g].Size;
                    }
                }

                this._buckets[0].ItemsList = 0;
                for (i = 1; i < this._nbuckets; i++)
                {
                    this._buckets[i].ItemsList = this._buckets[i - 1].ItemsList + this._buckets[i - 1].Size;
                    this._buckets[i - 1].Size = 0;
                }

                this._buckets[i - 1].Size = 0;
                for (i = 0; i < this._m; i++)
                {
                    if (!this.BucketsInsert(mapItems, i))
                    {
                        break;
                    }
                }

                if (i == this._m)
                {
                    return true; // SUCCESS
                }

                if (mappingIterations == 0)
                {
                    return false;
                }
            }
        }

        public BucketSortedList[] OrderingPhase(uint maxBucketSize)
        {
            var sortedLists = new BucketSortedList[maxBucketSize + 1];
            var inputBuckets = this._buckets;
            var inputItems = this._items;
            uint i;
            uint bucketSize, position;

            for (i = 0; i < this._nbuckets; i++)
            {
                bucketSize = inputBuckets[i].Size;
                if (bucketSize == 0)
                {
                    continue;
                }

                sortedLists[bucketSize].Size++;
            }

            sortedLists[1].BucketsList = 0;
            // Determine final position of list of buckets into the contiguous array that will store all the buckets
            for (i = 2; i <= maxBucketSize; i++)
            {
                sortedLists[i].BucketsList = sortedLists[i - 1].BucketsList + sortedLists[i - 1].Size;
                sortedLists[i - 1].Size = 0;
            }

            sortedLists[i - 1].Size = 0;
            // Store the buckets in a new array which is sorted by bucket sizes
            var outputBuckets = new Bucket[this._nbuckets];

            for (i = 0; i < this._nbuckets; i++)
            {
                bucketSize = inputBuckets[i].Size;
                if (bucketSize == 0)
                {
                    continue;
                }

                position = sortedLists[bucketSize].BucketsList + sortedLists[bucketSize].Size;
                outputBuckets[position].BucketID = i;
                outputBuckets[position].ItemsList = inputBuckets[i].ItemsList;
                sortedLists[bucketSize].Size++;
            }

            this._buckets = outputBuckets;

            // Store the items according to the new order of buckets.
            var outputItems = new Item[this._n];
            position = 0;

            for (bucketSize = 1; bucketSize <= maxBucketSize; bucketSize++)
            {
                for (i = sortedLists[bucketSize].BucketsList;
                     i < sortedLists[bucketSize].Size + sortedLists[bucketSize].BucketsList;
                     i++)
                {
                    var position2 = outputBuckets[i].ItemsList;
                    outputBuckets[i].ItemsList = position;
                    for (uint j = 0; j < bucketSize; j++)
                    {
                        outputItems[position].F = inputItems[position2].F;
                        outputItems[position].H = inputItems[position2].H;
                        position++;
                        position2++;
                    }
                }
            }

            // Return the items sorted in new order and free the old items sorted in old order
            this._items = outputItems;
            return sortedLists;
        }

        private bool PlaceBucketProbe(uint probe0Num, uint probe1Num, uint bucketNum, uint size, BitArray occupTable)
        {
            uint i;
            uint position;

            var p = this._buckets[bucketNum].ItemsList;

            // try place bucket with probe_num
            for (i = 0; i < size; i++) // placement
            {
                position = (uint)((this._items[p].F + ((ulong)this._items[p].H) * probe0Num + probe1Num) % this._n);
                if (occupTable.GetBit(position))
                {
                    break;
                }

                occupTable.SetBit(position);
                p++;
            }

            if (i != size) // Undo the placement
            {
                p = this._buckets[bucketNum].ItemsList;
                for (; ;)
                {
                    if (i == 0)
                    {
                        break;
                    }

                    position = (uint)((this._items[p].F + ((ulong)this._items[p].H) * probe0Num + probe1Num) % this._n);
                    occupTable.UnSetBit(position);

                    // 				([position/32]^=(1<<(position%32));
                    p++;
                    i--;
                }

                return false;
            }

            return true;
        }

        public bool SearchingPhase(uint maxBucketSize, BucketSortedList[] sortedLists, uint[] dispTable)
        {
            var maxProbes = (uint)(((Math.Log(this._m) / Math.Log(2.0)) / 20) * MaxProbesBase);
            uint i;
            var occupTable = new BitArray((int)(((this._n + 31) / 32) * sizeof(uint)));

            for (i = maxBucketSize; i > 0; i--)
            {
                uint probeNum = 0;
                uint probe0Num = 0;
                uint probe1Num = 0;
                var sortedListSize = sortedLists[i].Size;
                while (sortedLists[i].Size != 0)
                {
                    var currBucket = sortedLists[i].BucketsList;
                    uint nonPlacedBucket = 0;
                    for (uint j = 0; j < sortedLists[i].Size; j++)
                    {
                        // if bucket is successfully placed remove it from list
                        if (this.PlaceBucketProbe(probe0Num, probe1Num, currBucket, i, occupTable))
                        {
                            dispTable[this._buckets[currBucket].BucketID] = probe0Num + probe1Num * this._n;
                        }
                        else
                        {
                            this._buckets[nonPlacedBucket + sortedLists[i].BucketsList].ItemsList = this._buckets[currBucket].ItemsList;
                            this._buckets[nonPlacedBucket + sortedLists[i].BucketsList].BucketID = this._buckets[currBucket].BucketID;
                            nonPlacedBucket++;
                        }

                        currBucket++;
                    }

                    sortedLists[i].Size = nonPlacedBucket;
                    probe0Num++;
                    if (probe0Num >= this._n)
                    {
                        probe0Num -= this._n;
                        probe1Num++;
                    }

                    probeNum++;
                    if (probeNum < maxProbes && probe1Num < this._n)
                    {
                        continue;
                    }

                    sortedLists[i].Size = sortedListSize;
                    return false;
                }

                sortedLists[i].Size = sortedListSize;
            }

            return true;
        }
    }
}