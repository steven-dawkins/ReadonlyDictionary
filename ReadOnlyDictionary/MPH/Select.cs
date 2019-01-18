﻿/* ........................................................................ *
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
    internal class Select
    {
        #region <Tables>
        static readonly byte[] RankLookupTable = new byte[]{
                                                               0 , 1 , 1 , 2 , 1 , 2 , 2 , 3 , 1 , 2 , 2 , 3 , 2 , 3 , 3 , 4
                                                               ,  1 , 2 , 2 , 3 , 2 , 3 , 3 , 4 , 2 , 3 , 3 , 4 , 3 , 4 , 4 , 5
                                                               ,  1 , 2 , 2 , 3 , 2 , 3 , 3 , 4 , 2 , 3 , 3 , 4 , 3 , 4 , 4 , 5
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  1 , 2 , 2 , 3 , 2 , 3 , 3 , 4 , 2 , 3 , 3 , 4 , 3 , 4 , 4 , 5
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  3 , 4 , 4 , 5 , 4 , 5 , 5 , 6 , 4 , 5 , 5 , 6 , 5 , 6 , 6 , 7
                                                               ,  1 , 2 , 2 , 3 , 2 , 3 , 3 , 4 , 2 , 3 , 3 , 4 , 3 , 4 , 4 , 5
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  3 , 4 , 4 , 5 , 4 , 5 , 5 , 6 , 4 , 5 , 5 , 6 , 5 , 6 , 6 , 7
                                                               ,  2 , 3 , 3 , 4 , 3 , 4 , 4 , 5 , 3 , 4 , 4 , 5 , 4 , 5 , 5 , 6
                                                               ,  3 , 4 , 4 , 5 , 4 , 5 , 5 , 6 , 4 , 5 , 5 , 6 , 5 , 6 , 6 , 7
                                                               ,  3 , 4 , 4 , 5 , 4 , 5 , 5 , 6 , 4 , 5 , 5 , 6 , 5 , 6 , 6 , 7
                                                               ,  4 , 5 , 5 , 6 , 5 , 6 , 6 , 7 , 5 , 6 , 6 , 7 , 6 , 7 , 7 , 8
};

        static readonly byte[,] SelectLookupTable = new byte[256, 8] {
                                                                         { 255 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 3 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 255 , 255 , 255 , 255 } ,
                                                                         { 4 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 255 , 255 , 255 , 255 } ,
                                                                         { 3 , 4 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 255 , 255 , 255 } ,
                                                                         { 5 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 5 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 5 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 5 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 5 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 5 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 3 , 5 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 5 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 5 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 5 , 255 , 255 , 255 } ,
                                                                         { 4 , 5 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 5 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 5 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 5 , 255 , 255 , 255 } ,
                                                                         { 3 , 4 , 5 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 5 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 5 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 5 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 5 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 5 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 5 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 5 , 255 , 255 } ,
                                                                         { 6 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 6 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 6 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 3 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 6 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 6 , 255 , 255 , 255 } ,
                                                                         { 4 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 6 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 6 , 255 , 255 , 255 } ,
                                                                         { 3 , 4 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 6 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 6 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 6 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 6 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 6 , 255 , 255 } ,
                                                                         { 5 , 6 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 5 , 6 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 5 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 5 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 5 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 5 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 3 , 5 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 5 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 5 , 6 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 5 , 6 , 255 , 255 } ,
                                                                         { 4 , 5 , 6 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 5 , 6 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 5 , 6 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 5 , 6 , 255 , 255 } ,
                                                                         { 3 , 4 , 5 , 6 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 5 , 6 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 5 , 6 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 5 , 6 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 5 , 6 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 5 , 6 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 5 , 6 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 5 , 6 , 255 } ,
                                                                         { 7 , 255 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 3 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 7 , 255 , 255 , 255 } ,
                                                                         { 4 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 7 , 255 , 255 , 255 } ,
                                                                         { 3 , 4 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 7 , 255 , 255 } ,
                                                                         { 5 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 5 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 5 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 5 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 5 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 5 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 3 , 5 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 5 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 5 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 5 , 7 , 255 , 255 } ,
                                                                         { 4 , 5 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 5 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 5 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 5 , 7 , 255 , 255 } ,
                                                                         { 3 , 4 , 5 , 7 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 5 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 5 , 7 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 5 , 7 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 5 , 7 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 5 , 7 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 5 , 7 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 5 , 7 , 255 } ,
                                                                         { 6 , 7 , 255 , 255 , 255 , 255 , 255 , 255 } , { 0 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 1 , 6 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 2 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 2 , 6 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 2 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 3 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 3 , 6 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 3 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 3 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 3 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 3 , 6 , 7 , 255 , 255 } ,
                                                                         { 4 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 4 , 6 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 4 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 4 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 4 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 4 , 6 , 7 , 255 , 255 } ,
                                                                         { 3 , 4 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 3 , 4 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 3 , 4 , 6 , 7 , 255 , 255 } ,
                                                                         { 2 , 3 , 4 , 6 , 7 , 255 , 255 , 255 } , { 0 , 2 , 3 , 4 , 6 , 7 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 6 , 7 , 255 , 255 } , { 0 , 1 , 2 , 3 , 4 , 6 , 7 , 255 } ,
                                                                         { 5 , 6 , 7 , 255 , 255 , 255 , 255 , 255 } , { 0 , 5 , 6 , 7 , 255 , 255 , 255 , 255 } ,
                                                                         { 1 , 5 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 1 , 5 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 2 , 5 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 2 , 5 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 2 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 2 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 3 , 5 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 3 , 5 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 3 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 3 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 2 , 3 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 2 , 3 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 1 , 2 , 3 , 5 , 6 , 7 , 255 , 255 } , { 0 , 1 , 2 , 3 , 5 , 6 , 7 , 255 } ,
                                                                         { 4 , 5 , 6 , 7 , 255 , 255 , 255 , 255 } , { 0 , 4 , 5 , 6 , 7 , 255 , 255 , 255 } ,
                                                                         { 1 , 4 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 1 , 4 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 2 , 4 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 2 , 4 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 1 , 2 , 4 , 5 , 6 , 7 , 255 , 255 } , { 0 , 1 , 2 , 4 , 5 , 6 , 7 , 255 } ,
                                                                         { 3 , 4 , 5 , 6 , 7 , 255 , 255 , 255 } , { 0 , 3 , 4 , 5 , 6 , 7 , 255 , 255 } ,
                                                                         { 1 , 3 , 4 , 5 , 6 , 7 , 255 , 255 } , { 0 , 1 , 3 , 4 , 5 , 6 , 7 , 255 } ,
                                                                         { 2 , 3 , 4 , 5 , 6 , 7 , 255 , 255 } , { 0 , 2 , 3 , 4 , 5 , 6 , 7 , 255 } ,
                                                                         { 1 , 2 , 3 , 4 , 5 , 6 , 7 , 255 } , { 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 } };
        #endregion

        static void Insert0(ref uint buffer)
        {
            buffer = buffer >> 1;
        }

        static void Insert1(ref uint buffer)
        {
            buffer = buffer >> 1;
            buffer |= 0x80000000;
        }

        uint _n, _m;
        uint[] _bitsVec;
        uint[] _selectTable;

        public void Generate(uint[] keysVec, uint n, uint m)
        {
            uint buffer = 0;
            _n = n;
            _m = m;
            var nbits = _n + _m;
            var vecSize = (nbits + 0x1f) >> 5;
            var selTableSize = (_n >> 7) + 1;
            _bitsVec = new uint[vecSize];
            _selectTable = new uint[selTableSize];
            var j = 0;
            var i = j;
            var idx = i;
            for (; ; )
            {
                while (keysVec[j] == i)
                {
                    Insert1(ref buffer);
                    idx++;

                    if ((idx & 0x1f) == 0)
                        _bitsVec[(idx >> 5) - 1] = buffer; // (idx >> 5) = idx/32
                    j++;

                    if (j == _n) break;
                }

                if (i == _m)
                    break;

                while (keysVec[j] > i)
                {
                    Insert0(ref buffer);
                    idx++;

                    if ((idx & 0x1f) == 0) // (idx & 0x1f) = idx % 32
                        _bitsVec[(idx >> 5) - 1] = buffer; // (idx >> 5) = idx/32
                    i++;
                }
            }

            if ((idx & 0x1f) != 0)
            {
                buffer = buffer >> 0x20 - (idx & 0x1f);
                _bitsVec[(idx - 1) >> 5] = buffer;
            }

            GenerateSelTable();
        }

        unsafe void GenerateSelTable()
        {
            fixed (uint* pptrBitsVec = &(_bitsVec[0]))
            {
                var bitsTable = (byte*)pptrBitsVec;
                uint selTableIdx = 0;
                uint oneIdx = selTableIdx;
                uint vecIdx = oneIdx;
                uint partSum = vecIdx;
                while (oneIdx < _n)
                {
                    uint oldPartSum;
                    do
                    {
                        oldPartSum = partSum;
                        partSum += RankLookupTable[bitsTable[vecIdx]];
                        vecIdx++;
                    }
                    while (partSum <= oneIdx);

                    _selectTable[selTableIdx] = SelectLookupTable[bitsTable[vecIdx - 1], oneIdx - oldPartSum] + ((vecIdx - 1) << 3);
                    oneIdx += 0x80;
                    selTableIdx++;
                }
            }

        }

        unsafe public uint Query(uint oneIdx)
        {
            fixed (uint* pptrBitsVec = &(_bitsVec[0]))
            {
                uint oldPartSum;
                var bitsTable = (byte*)pptrBitsVec;
                var vecBitIdx = _selectTable[oneIdx >> 7];
                var vecByteIdx = vecBitIdx >> 3;
                oneIdx &= 0x7f;
                oneIdx += RankLookupTable[bitsTable[vecByteIdx] & ((1 << (int)(vecBitIdx & 7)) - 1)];
                uint partSum = 0;
                do
                {
                    oldPartSum = partSum;
                    partSum += RankLookupTable[bitsTable[vecByteIdx]];
                    vecByteIdx++;
                }
                while (partSum <= oneIdx);
                return SelectLookupTable[bitsTable[vecByteIdx - 1], oneIdx - oldPartSum] + ((vecByteIdx - 1) << 3);
            }
        }

        unsafe public uint NextQuery(uint vecBitIdx)
        {
            fixed (uint* pptrBitsVec = &(_bitsVec[0]))
            {
                uint oldPartSum;
                var bitsTable = (byte*)pptrBitsVec;
                var vecByteIdx = vecBitIdx >> 3;
                var oneIdx =
                    (uint)(RankLookupTable[bitsTable[vecByteIdx] & ((1 << (int)(vecBitIdx & 7)) - 1)] + 1);
                uint partSum = 0;
                do
                {
                    oldPartSum = partSum;
                    partSum += RankLookupTable[bitsTable[vecByteIdx]];
                    vecByteIdx++;
                } while (partSum <= oneIdx);
                return
                    (SelectLookupTable[bitsTable[vecByteIdx - 1], oneIdx - oldPartSum] + ((vecByteIdx - 1) << 3));
            }
        }
    }
}