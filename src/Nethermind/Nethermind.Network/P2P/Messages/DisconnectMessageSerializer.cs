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

using System.Linq;
using Nethermind.Core.Extensions;
using Nethermind.Serialization.Rlp;
using Nethermind.Stats.Model;

namespace Nethermind.Network.P2P.Messages
{
    public class DisconnectMessageSerializer : IMessageSerializer<DisconnectMessage>
    {
        public byte[] Serialize(DisconnectMessage msg)
        {
            return Rlp.Encode(
                Rlp.Encode((byte)msg.Reason) // sic!, as a list of 1 element
            ).Bytes;
        }

        private byte[] breach1 = Bytes.FromHexString("0204c104");
        private byte[] breach2 = Bytes.FromHexString("0204c180");

        public DisconnectMessage Deserialize(byte[] msgBytes)
        {
            if (msgBytes.Length == 1)
            {
                return new DisconnectMessage((DisconnectReason)msgBytes[0]);
            }

            if (msgBytes.SequenceEqual(breach1))
            {
                return new DisconnectMessage(DisconnectReason.Breach1);
            }

            if (msgBytes.SequenceEqual(breach2))
            {
                return new DisconnectMessage(DisconnectReason.Breach2);
            }

            RlpStream rlpStream = msgBytes.AsRlpStream();
            rlpStream.ReadSequenceLength();
            int reason = rlpStream.DecodeInt();
            DisconnectMessage disconnectMessage = new(reason);
            return disconnectMessage;
        }
    }
}
