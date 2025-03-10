//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Specs.Forks
{
    public class Byzantium : SpuriousDragon
    {
        private static IReleaseSpec _instance;

        protected Byzantium()
        {
            Name = "Byzantium";
            BlockReward = UInt256.Parse("3000000000000000000");
            DifficultyBombDelay = 3000000L;
            IsEip100Enabled = true;
            IsEip140Enabled = true;
            IsEip196Enabled = true;
            IsEip197Enabled = true;
            IsEip198Enabled = true;
            IsEip211Enabled = true;
            IsEip214Enabled = true;
            IsEip649Enabled = true;
            IsEip658Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Byzantium());
    }
}
