﻿/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Store;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Evm.Test
{
    [TestFixture]
    public class StorageProviderTests
    {
        private readonly Address _address1 = new Address(Keccak.Compute("1"));
        private readonly Address _address2 = new Address(Keccak.Compute("2"));

        private readonly IStateProvider _stateProvider = new StateProvider(new StateTree(new InMemoryDb()), ShouldLog.State ? new ConsoleLogger() : null, Substitute.For<IDb>());

        [SetUp]
        public void Setup()
        {
            _stateProvider.CreateAccount(_address1, 0);
            _stateProvider.CreateAccount(_address2, 0);
            _stateProvider.Commit(Frontier.Instance);
        }

        private readonly byte[][] _values =
        {
            new byte[] {0},
            new byte[] {1},
            new byte[] {2},
            new byte[] {3},
            new byte[] {4},
            new byte[] {5},
            new byte[] {6},
            new byte[] {7},
            new byte[] {8},
            new byte[] {9},
            new byte[] {10},
            new byte[] {11},
            new byte[] {12},
        };

        [Test]
        public void Empty_commit_restore()
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Commit(Frontier.Instance);
            provider.Restore(-1);
        }

        private StorageProvider BuildStorageProvider()
        {
            ILogger stateLogger = ShouldLog.State ? new ConsoleLogger() : null;
            StorageProvider provider = new StorageProvider(new DbProvider(stateLogger), _stateProvider, stateLogger);
            return provider;
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Same_address_same_index_different_values_restore(int snapshot)
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 1, _values[2]);
            provider.Set(_address1, 1, _values[3]);
            provider.Restore(snapshot);

            Assert.AreEqual(_values[snapshot + 1], provider.Get(_address1, 1));
        }

        [Test]
        public void Keep_in_cache()
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Set(_address1, 1, _values[1]);
            provider.Commit(Frontier.Instance);
            provider.Get(_address1, 1);
            provider.Set(_address1, 1, _values[2]);
            provider.Restore(-1);
            provider.Set(_address1, 1, _values[2]);
            provider.Restore(-1);
            provider.Set(_address1, 1, _values[2]);
            provider.Restore(-1);
            Assert.AreEqual(_values[1], provider.Get(_address1, 1));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Same_address_different_index(int snapshot)
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 2, _values[2]);
            provider.Set(_address1, 3, _values[3]);
            provider.Restore(snapshot);

            Assert.AreEqual(_values[Math.Min(snapshot + 1, 1)], provider.Get(_address1, 1));
        }

        [Test]
        public void Commit_restore()
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 2, _values[2]);
            provider.Set(_address1, 3, _values[3]);
            provider.Commit(Frontier.Instance);
            provider.Set(_address2, 1, _values[4]);
            provider.Set(_address2, 2, _values[5]);
            provider.Set(_address2, 3, _values[6]);
            provider.Commit(Frontier.Instance);
            provider.Set(_address1, 1, _values[7]);
            provider.Set(_address1, 2, _values[8]);
            provider.Set(_address1, 3, _values[9]);
            provider.Commit(Frontier.Instance);
            provider.Set(_address2, 1, _values[10]);
            provider.Set(_address2, 2, _values[11]);
            provider.Set(_address2, 3, _values[12]);
            provider.Commit(Frontier.Instance);
            provider.Restore(-1);

            Assert.AreEqual(_values[7], provider.Get(_address1, 1));
            Assert.AreEqual(_values[8], provider.Get(_address1, 2));
            Assert.AreEqual(_values[9], provider.Get(_address1, 3));
            Assert.AreEqual(_values[10], provider.Get(_address2, 1));
            Assert.AreEqual(_values[11], provider.Get(_address2, 2));
            Assert.AreEqual(_values[12], provider.Get(_address2, 3));
        }

        [Test]
        public void Commit_no_changes()
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 2, _values[2]);
            provider.Set(_address1, 3, _values[3]);
            provider.Restore(-1);
            provider.Commit(Frontier.Instance);

            Assert.AreEqual(Keccak.EmptyTreeHash, provider.GetRoot(_address1));
        }

        [Test]
        public void Commit_no_changes_2()
        {
            StorageProvider provider = BuildStorageProvider();
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 1, _values[2]);
            provider.Set(_address1, 1, _values[3]);
            provider.Restore(2);
            provider.Restore(1);
            provider.Restore(0);
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Set(_address1, 1, _values[1]);
            provider.Set(_address1, 1, _values[2]);
            provider.Set(_address1, 1, _values[3]);
            provider.Restore(-1);
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Get(_address1, 1);
            provider.Commit(Frontier.Instance);

            Assert.AreEqual(Keccak.EmptyTreeHash, provider.GetRoot(_address1));
        }
        
        [Test]
        public void Commit_clear_caches_get_previous_root()
        {
            // block 1
            StorageProvider storageProvider = BuildStorageProvider();
            storageProvider.Set(_address1, 1, _values[1]);
            storageProvider.Commit(Frontier.Instance);
            _stateProvider.Commit(Frontier.Instance);
            
            // block 2
            Keccak stateRoot = _stateProvider.StateRoot;
            storageProvider.Set(_address1, 1, _values[2]);
            storageProvider.Commit(Frontier.Instance);
            _stateProvider.Commit(Frontier.Instance);
            
            // revert
            _stateProvider.ClearCaches();
            storageProvider.ClearCaches();
            _stateProvider.StateRoot = stateRoot;
            
            byte[] valueAfter = storageProvider.Get(_address1, 1);
            
            Assert.AreEqual(_values[1], valueAfter);
        }
        
        [Test]
        public void Can_commit_when_exactly_at_capacity_regression()
        {
            // block 1
            StorageProvider storageProvider = BuildStorageProvider();
            for (int i = 0; i < StorageProvider.StartCapacity; i++)
            {
                storageProvider.Set(_address1, 1, _values[i % 2]);
            }
            
            storageProvider.Commit(Frontier.Instance);
            _stateProvider.Commit(Frontier.Instance);
            
            byte[] valueAfter = storageProvider.Get(_address1, 1);
            Assert.AreEqual(_values[(StorageProvider.StartCapacity + 1) % 2], valueAfter);
        }
    }
}