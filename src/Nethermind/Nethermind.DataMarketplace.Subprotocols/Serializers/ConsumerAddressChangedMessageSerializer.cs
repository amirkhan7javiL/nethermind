//  Copyright (c) 2018 Demerzel Solutions Limited
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

using System.IO;
using Nethermind.Core;
using Nethermind.DataMarketplace.Subprotocols.Messages;
using Nethermind.Network;
using Nethermind.Serialization.Rlp;

namespace Nethermind.DataMarketplace.Subprotocols.Serializers
{
    public class ConsumerAddressChangedMessageSerializer : IMessageSerializer<ConsumerAddressChangedMessage>
    {
        public byte[] Serialize(ConsumerAddressChangedMessage message)
            => Rlp.Encode(Rlp.Encode(message.Address)).Bytes;

        public ConsumerAddressChangedMessage Deserialize(byte[] bytes)
        {
            RlpStream context = bytes.AsRlpStream();
            context.ReadSequenceLength();
            Address? address = context.DecodeAddress();
            if (address == null)
            {
                throw new InvalidDataException($"{nameof(ConsumerAddressChangedMessage)}.{nameof(ConsumerAddressChangedMessage.Address)} cannot be null");
            }

            return new ConsumerAddressChangedMessage(address);
        }
    }
}
