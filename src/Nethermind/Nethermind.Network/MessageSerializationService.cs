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

using System;
using System.Collections.Concurrent;
using System.Reflection;
using DotNetty.Buffers;
using Nethermind.Network.P2P;
using Nethermind.Network.P2P.Messages;
using Nethermind.Network.P2P.Subprotocols.Les;

namespace Nethermind.Network
{
    public class MessageSerializationService : IMessageSerializationService
    {
        private readonly ConcurrentDictionary<RuntimeTypeHandle, object> _serializers = new();
        private readonly ConcurrentDictionary<RuntimeTypeHandle, object> _zeroSerializers = new();

        public T Deserialize<T>(byte[] bytes) where T : MessageBase
        {
            if (!TryGetSerializer(out IMessageSerializer<T> messageSerializer))
            {
                throw new InvalidOperationException($"No {nameof(IMessageSerializer<T>)} registered for {typeof(T).Name}.");
            }

            return messageSerializer.Deserialize(bytes);
        }

        public T Deserialize<T>(IByteBuffer buffer) where T : MessageBase
        {
            if (TryGetZeroSerializer(out IZeroMessageSerializer<T> zeroMessageSerializer))
            {
                return zeroMessageSerializer.Deserialize(buffer);
            }

            // 3% allocation of a sample run of Goerli 3 million blocks fast sync on buffer.ReadAllBytes
            // this can be improved by adding ZeroMessageSerializer for a new message type
            return Deserialize<T>(buffer.ReadAllBytes());

        }

        public void Register(Assembly assembly)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                Type[] implementedInterfaces = type.GetInterfaces();
                foreach (Type implementedInterface in implementedInterfaces)
                {
                    if (!implementedInterface.IsGenericType)
                    {
                        continue;
                    }

                    Type interfaceGenericDefinition = implementedInterface.GetGenericTypeDefinition();
                    if (interfaceGenericDefinition == typeof(IMessageSerializer<>).GetGenericTypeDefinition())
                    {
                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        if (constructor == null)
                        {
                            continue;
                        }

                        _serializers[implementedInterface.GenericTypeArguments[0].TypeHandle] = Activator.CreateInstance(type);
                    }

                    if (interfaceGenericDefinition == typeof(IZeroMessageSerializer<>).GetGenericTypeDefinition())
                    {
                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        if (constructor == null)
                        {
                            continue;
                        }

                        _zeroSerializers[implementedInterface.GenericTypeArguments[0].TypeHandle] = Activator.CreateInstance(type);
                    }
                }
            }
        }

        public void Register<T>(IMessageSerializer<T> messageSerializer) where T : MessageBase
        {
            _serializers[typeof(T).TypeHandle] = messageSerializer;
        }

        public void Register<T>(IZeroMessageSerializer<T> messageSerializer) where T : MessageBase
        {
            _zeroSerializers[typeof(T).TypeHandle] = messageSerializer;
        }

        public IByteBuffer ZeroSerialize<T>(T message) where T : MessageBase
        {
            void WriteAdaptivePacketType(in IByteBuffer buffer)
            {
                if (message is P2PMessage p2PMessage)
                {
                    buffer.WriteByte(p2PMessage.AdaptivePacketType);
                }
            }

            int p2pMessageLength = (message is P2PMessage ? sizeof(int) : 0);
            if (TryGetZeroSerializer(out IZeroMessageSerializer<T> zeroMessageSerializer))
            {
                IByteBuffer byteBuffer = PooledByteBufferAllocator.Default.Buffer(
                    zeroMessageSerializer is IZeroInnerMessageSerializer<T> zeroInnerMessageSerializer
                        ? zeroInnerMessageSerializer.GetLength(message, out _) + p2pMessageLength
                        : 64);
                WriteAdaptivePacketType(byteBuffer);
                zeroMessageSerializer.Serialize(byteBuffer, message);
                return byteBuffer;
            }
            else
            {
                byte[] serialized = Serialize(message);
                IByteBuffer byteBuffer = PooledByteBufferAllocator.Default.Buffer(serialized.Length + p2pMessageLength);
                WriteAdaptivePacketType(byteBuffer);
                byteBuffer.WriteBytes(serialized);
                return byteBuffer;
            }
        }
        public byte[] Serialize<T>(T messageBase) where T : MessageBase
        {
            return TryGetSerializer(out IMessageSerializer<T> messageSerializer)
                ? messageSerializer.Serialize(messageBase)
                : throw new InvalidOperationException($"No {nameof(IMessageSerializer<T>)} registered for {typeof(T).Name}.");
        }

        private bool TryGetZeroSerializer<T>(out IZeroMessageSerializer<T> serializer) where T : MessageBase
        {
            RuntimeTypeHandle typeHandle = typeof(T).TypeHandle;
            if (!_zeroSerializers.TryGetValue(typeHandle, out object serializerObject))
            {
                serializer = null;
                return false;
            }

            if (serializerObject is IZeroMessageSerializer<T> messageSerializer)
            {
                serializer = messageSerializer;
                return true;
            }

            throw new InvalidOperationException($"Zero serializer for {nameof(T)} (registered: {serializerObject?.GetType().Name}) does not implement required interfaces");
        }

        private bool TryGetSerializer<T>(out IMessageSerializer<T> serializer) where T : MessageBase
        {
            RuntimeTypeHandle typeHandle = typeof(T).TypeHandle;
            if (!_serializers.TryGetValue(typeHandle, out object serializerObject))
            {
                serializer = null;
                return false;
            }

            if (!(serializerObject is IMessageSerializer<T> messageSerializer))
            {
                throw new InvalidOperationException($"Serializer for {nameof(T)} (registered: {serializerObject?.GetType().Name}) does not implement required interfaces");
            }

            serializer = messageSerializer;
            return true;
        }
    }
}
