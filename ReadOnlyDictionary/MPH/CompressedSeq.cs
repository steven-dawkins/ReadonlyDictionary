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

    [Serializable]
    internal class CompressedSeq
    {
        uint[] _lengthRems;
        uint _n;
        uint _remR;
        Select _sel;
        uint[] _storeTable;
        uint _totalLength;

        static uint ILog2(uint x)
        {
            uint res = 0;

            while (x > 1)
            {
                x >>= 1;
                res++;
            }

            return res;
        }

        public void Generate(uint[] valsTable, uint n)
        {
            uint i;
            // lengths: represents lengths of encoded values
            var lengths = new uint[n];

            this._n = n;
            this._totalLength = 0;

            for (i = 0; i < this._n; i++)
            {
                if (valsTable[i] == 0)
                {
                    lengths[i] = 0;
                }
                else
                {
                    lengths[i] = ILog2(valsTable[i] + 1);
                    this._totalLength += lengths[i];
                }
            }

            this._storeTable = new uint[(this._totalLength + 31) >> 5];
            this._totalLength = 0;

            for (i = 0; i < this._n; i++)
            {
                if (valsTable[i] == 0)
                    continue;
                var storedValue = valsTable[i] - ((1U << (int)lengths[i]) - 1U);
                BitBool.SetBitsAtPos(this._storeTable, this._totalLength, storedValue, lengths[i]);
                this._totalLength += lengths[i];
            }

            this._remR = ILog2(this._totalLength / this._n);

            if (this._remR == 0)
            {
                this._remR = 1;
            }

            this._lengthRems = new uint[((this._n * this._remR) + 0x1f) >> 5];

            var remsMask = (1U << (int)this._remR) - 1U;
            this._totalLength = 0;

            for (i = 0; i < this._n; i++)
            {
                this._totalLength += lengths[i];
                BitBool.SetBitsValue(this._lengthRems, i, this._totalLength & remsMask, this._remR, remsMask);
                lengths[i] = this._totalLength >> (int)this._remR;
            }

            this._sel = new Select();

            this._sel.Generate(lengths, this._n, (this._totalLength >> (int)this._remR));
        }

        public uint Query(uint idx)
        {
            uint selRes;
            uint encIdx;
            var remsMask = (uint)((1 << (int)this._remR) - 1);

            if (idx == 0)
            {
                encIdx = 0;
                selRes = this._sel.Query(idx);
            }
            else
            {
                selRes = this._sel.Query(idx - 1);
                encIdx = (selRes - (idx - 1)) << (int)this._remR;
                encIdx += BitBool.GetBitsValue(this._lengthRems, idx - 1, this._remR, remsMask);
                selRes = this._sel.NextQuery(selRes);
            }

            var encLength = (selRes - idx) << (int)this._remR;
            encLength += BitBool.GetBitsValue(this._lengthRems, idx, this._remR, remsMask);
            encLength -= encIdx;
            if (encLength == 0)
            {
                return 0;
            }

            return (BitBool.GetBitsAtPos(this._storeTable, encIdx, encLength) + ((uint)((1 << (int)encLength) - 1)));
        }
    }
}