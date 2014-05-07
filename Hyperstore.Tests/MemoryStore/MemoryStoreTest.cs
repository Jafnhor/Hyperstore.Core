// Copyright 2014 Zenasoft.  All rights reserved.
//
// This file is part of Hyperstore.
//
//    Hyperstore is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Hyperstore is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Hyperstore.  If not, see <http://www.gnu.org/licenses/>.
 
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Hyperstore.Modeling;
using System.Diagnostics;
using Hyperstore.Modeling.MemoryStore;
using System.Linq;
using Hyperstore.Tests.Model;
using Hyperstore.Modeling.HyperGraph;
using Hyperstore.Modeling.Metadata;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace Hyperstore.Tests.MemoryStore
{
    // http://eric.themoritzfamily.com/understanding-psqls-mvcc.html
    /// <summary>
    ///This is a test class for MvccPersistenceProviderTest and is intended
    ///to contain all MvccPersistenceProviderTest Unit Tests
    ///</summary>
    [TestClass]
    public class MemoryStoreTest : HyperstoreTestBase
    {
        class Data : IGraphNode, ICloneable<IGraphNode>
        {
            public Data(string key, int v)
            {
                Id = new Identity("test", key);
                Value = v;
            }

            public object Value { get; set; }

            public string GraphId
            {
                get;
                set;
            }

            public Identity Id
            {
                get;
                set;
            }

            public Identity SchemaId
            {
                get;
                set;
            }

            public Identity StartId
            {
                get;
                set;
            }

            public Identity StartSchemaId
            {
                get;
                set;
            }

            public Identity EndId
            {
                get;
                set;
            }

            public Identity EndSchemaId
            {
                get;
                set;
            }

            public long Version
            {
                get;
                set;
            }

            public NodeType NodeType
            {
                get;
                set;
            }

            public System.Collections.Generic.IEnumerable<EdgeInfo> GetEdges(Modeling.HyperGraph.Direction direction, ISchemaRelationship schemaRelationship)
            {
                throw new NotImplementedException();
            }

            public void AddEdge(Identity id, Identity metadataId, Modeling.HyperGraph.Direction direction)
            {
                throw new NotImplementedException();
            }

            public void RemoveEdge(Identity edgeId, Modeling.HyperGraph.Direction direction)
            {
                throw new NotImplementedException();
            }

            public IGraphNode Clone()
            {
                return new Data(Id.Key, (int)Value);
            }
        }

        /// <summary>
        /// Deadlock
        ///</summary>
        [TestMethod]
        public async Task DeadlockTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition(resolver => resolver.RegisterSetting(Setting.MaxTimeBeforeDeadlockInMs, 300)));
            var domain = await store.CreateDomainModelAsync("Test"); 

            var gate = new System.Threading.ManualResetEvent(false);

            var factory = new TaskFactory();

            var t1 = factory.StartNew(() =>
            {
                // Prend la ressource est la garde
                using (var s = store.BeginSession())
                {
                    s.AcquireLock(LockType.Exclusive, "a");
                    gate.Set();
                    Sleep(1000);
                }
            });

            var t2 = factory.StartNew(() =>
            {
                gate.WaitOne();
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    s.AcquireLock(LockType.Exclusive, "a");
                }
            });

            try
            {
                Task.WaitAll(t1, t2);
            }
            catch (AggregateException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(DeadLockException));
            }
        }


        [TestMethod]
        public async Task DeadlockTest2()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition(resolver => resolver.RegisterSetting(Setting.MaxTimeBeforeDeadlockInMs, 300)));
            var domain = await store.CreateDomainModelAsync("Test"); 

            var gate = new System.Threading.ManualResetEvent(false);

            var factory = new TaskFactory();

            var t1 = factory.StartNew(() =>
            {
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    gate.Set();
                    s.AcquireLock(LockType.Exclusive, "a");
                    Sleep(150);
                }
            });
            Sleep(50);

            var t2 = factory.StartNew(() =>
            {
                gate.WaitOne();
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    // Récupération du lock mais la transaction t1 à fait un rollback
                    // on peu donc continuer sans erreur   
                    s.AcquireLock(LockType.Exclusive, "a");
                    // OK
                }
            });

            Task.WaitAll(t1, t2);
        }

        [TestMethod]
        public async Task SerializeTransactionExceptionTest()
        {
            // Accéder à une même donnée ds une session Serializable génere une SerializeTransactionException
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition(resolver => resolver.RegisterSetting(Setting.MaxTimeBeforeDeadlockInMs, 100)));
            var domain = await store.CreateDomainModelAsync("Test"); 

            var gate = new System.Threading.ManualResetEvent(false);

            var factory = new TaskFactory();

            var t1 = factory.StartNew(() =>
            {
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    s.AcquireLock(LockType.Exclusive, "a");
                    gate.Set();
                    Sleep(200);
                    s.AcceptChanges();
                }
            });

            var t2 = factory.StartNew(() =>
            {
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    gate.WaitOne();
                    // Récupération du lock mais la transaction t1 à fait un commit
                    // on obtient une SerializeTransactionException
                    Debug.WriteLine("T2 acquire");
                    s.AcquireLock(LockType.Exclusive, "a");
                }
            });

            try
            {
                Task.WaitAll(t1, t2);
                Assert.Inconclusive();
            }
            catch (AggregateException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(SerializableTransactionException));
            }
        }


        [TestMethod]
        public async Task AcquireExclusive2Test()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            var factory = new TaskFactory();

            var tasks = new Task[2];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = factory.StartNew(() =>
                {
                    using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                    {
                        s.AcquireLock(LockType.ExclusiveWait, "a");
                        Sleep(100);

                        s.AcceptChanges();
                    }

                });
            }

            Task.WaitAll(tasks);
#if TEST
            Assert.IsTrue(((LockManager)((IStore)store).LockManager).IsEmpty());
#endif
        }

        [TestMethod]
        public async Task AcquireExclusiveWaitTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test");
            var factory = new TaskFactory();

            var created = false;
            var cx = 0;
            var tasks = new Task[100];
            for (var i = 0; i < tasks.Length; i++)
            {
                var x = i;
                tasks[i] = factory.StartNew(() =>
                {
                    using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                    {
                        if (x % 2 == 0)
                        {
                            s.AcquireLock(LockType.Shared, "a");

                        }
                        else if (!created)
                        {
                            s.AcquireLock(LockType.ExclusiveWait, "a");

                            if (!created)
                            {
                                created = true;
                                cx++;
                                Sleep(200);
                            }
                        }
                        s.AcceptChanges();
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.AreEqual(1, cx);
#if TEST
            Assert.IsTrue(((LockManager)((IStore)store).LockManager).IsEmpty());
#endif

        }

        [TestMethod]
        public async Task NestedSession()
        {
            var store = new Store();
            store.DependencyResolver.Register<ITransactionManager>(new MockTransactionManager());
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                using (var s2 = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    provider.AddNode( new Data("1", 10), PrimitivesSchema.Int32Schema);
                    provider.UpdateNode(new Data("1", 20), PrimitivesSchema.Int32Schema);
                    s2.AcceptChanges();
                }
                s.AcceptChanges();
            }

            Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema).Value, 20);
        }

        [TestMethod]
        public async Task RollbackNestedSession()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                using (var s2 = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
                    provider.UpdateNode(new Data("1", 20), PrimitivesSchema.Int32Schema);
                }
            }

            using (var s = store.BeginSession())
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema), null);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task NestedSessionRollback()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test");
            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
                using (var s2 = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    provider.UpdateNode(new Data("1", 20), PrimitivesSchema.Int32Schema);
                }
                s.AcceptChanges();
            }

            using (var s = store.BeginSession())
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema), null);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task CommitTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
                s.AcceptChanges();
            }
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema).Value, 10);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task RollbackTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema), null);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task RollbackUpdateTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test");
            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
                s.AcceptChanges();
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.UpdateNode(new Data("1", 20), PrimitivesSchema.Int32Schema);
            }
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema).Value, 10);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task DeleteTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test");
            var provider = new TransactionalMemoryStore(domain);
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.AddNode(new Data("1", 10), PrimitivesSchema.Int32Schema);
                s.AcceptChanges();
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.RemoveNode(new Identity("test", "1"));
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema).Value, 10);
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                provider.RemoveNode(new Identity("test", "1"));
                s.AcceptChanges();
            }

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                Assert.AreEqual(provider.GetNode(new Identity("test", "1"), PrimitivesSchema.Int32Schema), null);
                s.AcceptChanges();
            }
        }

        [TestMethod]
        public async Task SerializableTransactionTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test");

            Identity aid;
            dynamic a;

            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                a = new DynamicModelEntity(domain, TestDomainDefinition.XExtendsBaseClass);
                aid = a.Id;
                a.Value = 10;
                s.AcceptChanges();
            }

            var factory = new TaskFactory();
            var signal = new System.Threading.ManualResetEventSlim();
            var signal2 = new System.Threading.ManualResetEventSlim();

            var t1 = factory.StartNew(() =>
            {
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.Serializable }))
                {
                    // Accès sur A pour ouvrir une transaction Serializable (la session ne fait rien). Ce n'est que qu'en on 
                    // accède à la donnée que la transaction est crèèe
                    var x = a.Value; // Ou n'importe quoi 
                    signal2.Set();
                    signal.Wait();
                    Assert.AreEqual(a.Value, 10); // On ne "voit" pas les modifications des autres transactions
                }
            });

            Sleep(50);

            var t2 = factory.StartNew(() =>
            {
                signal2.Wait();
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    a.Value = 11;
                    s.AcceptChanges();
                }
                signal.Set();
            });

            Task.WaitAll(t1, t2);
        }

        [TestMethod]
        public async Task ReadPhantomTest()
        {
            var store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition());
            var domain = await store.CreateDomainModelAsync("Test"); 

            Identity aid;
            dynamic a;

            // Création d'une valeur
            using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
            {
                a = new DynamicModelEntity(domain, TestDomainDefinition.XExtendsBaseClass);
                aid = a.Id;
                a.Value = 10;
                s.AcceptChanges();
            }

            var factory = new TaskFactory();
            var signal = new System.Threading.ManualResetEventSlim();
            var signal2 = new System.Threading.ManualResetEventSlim();

            var t1 = factory.StartNew(() =>
            {
                // Cette transaction démarre avant l'autre mais elle va pouvoir 'voir' ses modifs commités 
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    signal2.Set();
                    signal.Wait();// On attend le commit de l'autre                    
                    Assert.AreEqual(11, a.Value); // On "voit" dèja que la valeur a été modifiée
                }
            });

            Sleep(50);

            var t2 = factory.StartNew(() =>
            {
                signal2.Wait(); // On s'assure de démarrer aprés l'autre transaction
                using (var s = store.BeginSession(new SessionConfiguration { IsolationLevel = SessionIsolationLevel.ReadCommitted }))
                {
                    a.Value = 11;
                    s.AcceptChanges();
                }
                signal.Set();
            });

            Task.WaitAll(t1, t2);

        }
    }
}