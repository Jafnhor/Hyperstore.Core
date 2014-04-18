﻿using Hyperstore.Modeling;
using Hyperstore.Tests.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperstore.Bench
{
    class Program
    {
        private IHyperstore store;
        private ConcurrentDictionary<int, Identity> ids;
        static void Main(string[] args)
        {
            new Program().BenchWithConstraints().Wait();
        }

        public async Task BenchWithConstraints()
        {
            for(;;) {
            long nb = 0;
            store = new Store();
            await store.LoadSchemaAsync(new TestDomainDefinition("Hyperstore.Tests.Model"));

            var config = new DomainConfiguration()
                                .UsesIdGenerator(resolver => new Hyperstore.Modeling.Domain.LongIdGenerator());
            var domain = await store.CreateDomainModelAsync("Test", config);

                domain.Events.p
            var sw = new Stopwatch();

            // Ajout de 100 contraintes
            var nbc = 100;
            //for (int i = 0; i < nbc; i++)
            //    TestDomainDefinition.XExtendsBaseClass.AddImplicitConstraint(self => System.Threading.Interlocked.Increment(ref nb) > 0, "OK");

            Console.WriteLine("Running...");
            sw.Start();
            var mx = 10000;
            AddElement(domain, mx);
            Console.WriteLine("Add " + sw.ElapsedMilliseconds);
            sw.Restart();
            UpdateElement(mx);
            Console.WriteLine("Update " + sw.ElapsedMilliseconds);
            sw.Restart();
            ReadElement(mx);
            Console.WriteLine("Read " + sw.ElapsedMilliseconds);
            sw.Restart();
            RemoveElement(mx);
            Console.WriteLine("Remove " + sw.ElapsedMilliseconds);
            sw.Restart();
            sw.Stop();
            Console.WriteLine("Expected {0} Value {1}", mx * nbc * 2, nb);
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadKey();
            }
            
            //Assert.AreEqual(mx * nbc * 2, nb); // Nbre de fois la contrainte est appelée (sur le add et le update)
            //Assert.IsTrue(sw.ElapsedMilliseconds < 3000, String.Format("ElapsedTime = {0}", sw.ElapsedMilliseconds));
        }

        private void AddElement(IDomainModel domain, int max)
        {
            ids = new ConcurrentDictionary<int, Identity>();

            Parallel.For(0, max, i =>
            {
                using (var tx = store.BeginSession())
                {
                    var a = new XExtendsBaseClass(domain);
                    if (ids.TryAdd(i, ((IModelElement)a).Id))
                        tx.AcceptChanges();
                }
            });
        }

        private void UpdateElement(int max)
        {
            Parallel.For(0, max, i =>
            {
                using (var tx = store.BeginSession())
                {
                    var a = store.GetElement<XExtendsBaseClass>(ids[i]);
                    a.Name = "Toto" + i;
                    tx.AcceptChanges();
                }
            });
        }

        private void ReadElement(int max)
        {
            //Parallel.For(0, max, i =>
            for (int i = 0; i < max; i++)
            {
                using (var tx = store.BeginSession())
                {
                    var a = store.GetElement<XExtendsBaseClass>(ids[i]);
                    var x = a.Name;
                    tx.AcceptChanges();
                }
            }
            //);
        }

        private int RemoveElement(int max)
        {
            Parallel.For(0, max, i =>
            {
                using (var tx = store.BeginSession())
                {
                    IModelElement a = store.GetElement<XExtendsBaseClass>(ids[i]);
                    //if (a != null)
                    {
                        Identity id;
                        if (!ids.TryRemove(i, out id) || id != ((IModelElement)a).Id)
                            throw new Exception();
                        a.Remove();
                    }
                    tx.AcceptChanges();
                }
            }
            );

            var x = store.GetElements(TestDomainDefinition.XExtendsBaseClass).Count();
            var y = ids.Count();

            return x + y;
        }
    }
}