﻿using AutoMapper;
using Data.Dump.Persistence.Sql;
using Data.Dump.Schema.Mapping;
using Data.Dump.Sql.Sample.Data;
using Data.Dump.Sql.Sample.Models;
using Data.Dump.Sql.Sample.Models.Projection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace Data.Dump.Sql.Sample
{
    class Program
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["Dump"].ConnectionString;
        /*
         * Note: Running all the examples in one go has no point, as the tables of the same type would be overwritten when no name is specified.
         * So Pick one, and take your time to examine the results in the database. 
         *
         * I tried to choose some workable sizes for the datasets, so you don't have to wait forever until it completes. Feel free to experiment.
         * Running the simple poco sample for 1000000 items takes a little less then 30 seconds on my machine, with memory usage never exceeding 70mb.
         * Keep in mind that the nested examples generate an exponential amount of data for the nested lists, so beware.
         *
         * Be advised that enabling 'writeToConsole' to the console will jsonserialize the poco's, which takes considerable amounts of time. 
         *
         * Also, sorry for the ugly switch statement, but this is just an example project. Live with it ;)
         *
         */
        static void Main(string[] args)
        {
            var run = Run.DeeplyNestedExtended;
            var writeToConsole = false;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var repo = new SqlRepository(new SqlStore(ConnectionString));

            switch (run)
            {
                case Run.Simple:
                    var pocos = DataFactory.Pocos(10000);
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########POCOS##########");
                        Console.WriteLine(JsonConvert.SerializeObject(pocos, Formatting.Indented));
                    }
                    repo.Save(pocos);
                    break;

                case Run.SimpleNamedCollections:
                    var first = DataFactory.Pocos(100);
                    var second = DataFactory.Pocos(100);
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########FIRST##########");
                        Console.WriteLine(JsonConvert.SerializeObject(first, Formatting.Indented));
                        Console.WriteLine("##########SECOND##########");
                        Console.WriteLine(JsonConvert.SerializeObject(second, Formatting.Indented));
                    }
                    //you can pass in a name for the collection, so you can create different tables with the same model
                    repo.Save(first, "FirstCollection");
                    repo.Save(second, "SecondCollection");
                    break;

                case Run.Nested:
                    var nested = DataFactory.NestedPocos(1000);
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########NESTED##########");
                        Console.WriteLine(JsonConvert.SerializeObject(nested, Formatting.Indented));
                    }
                    repo.Save(nested, new FieldSelectorCollection<NestedPoco>()
                    {
                        new FieldSelector<NestedPoco, NestedPoco>(x => x),
                        new FieldSelector<NestedPoco, IEnumerable<Poco>, int>(
                            x => x.Pocos,
                            x => x.Id,
                            "NestedPocoId"
                        )
                    });
                    break;

                case Run.NestedNamedCollections:
                    var firstNested = DataFactory.NestedPocos(100);
                    var secondNested = DataFactory.NestedPocos(100);
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########FIRSTNESTED##########");
                        Console.WriteLine(JsonConvert.SerializeObject(firstNested, Formatting.Indented));
                        Console.WriteLine("##########SECONDNESTED##########");
                        Console.WriteLine(JsonConvert.SerializeObject(secondNested, Formatting.Indented));
                    }

                    //when creating datasets you can also pass in collection names
                    repo.Save(firstNested, new FieldSelectorCollection<NestedPoco>()
                    {
                        new FieldSelector<NestedPoco, NestedPoco>(x => x, "FirstNestedCollection"),
                        new FieldSelector<NestedPoco, IEnumerable<Poco>, int>(
                            x => x.Pocos,
                            "FirstSubCollection",
                            x => x.Id,
                            "NestedPocoId"
                        )
                    });

                    //you can even pass in a function to return the name
                    repo.Save(secondNested, new FieldSelectorCollection<NestedPoco>()
                    {
                        new FieldSelector<NestedPoco, NestedPoco>(x => x, () => "SecondNestedCollection"),
                        new FieldSelector<NestedPoco, IEnumerable<Poco>, int>(
                            x => x.Pocos,
                            () => "SecondSubCollection",
                            x => x.Id,
                            "NestedPocoId"
                        )
                    });
                    break;

                case Run.DeeplyNested:
                    var deeplyNested = DataFactory.DeeplyNestedPocos(100).ToList();
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########DEEPLYNESTED##########");
                        Console.WriteLine(JsonConvert.SerializeObject(deeplyNested, Formatting.Indented));
                    }
                    repo.Save(
                        deeplyNested.AsEnumerable(),
                        new FieldSelectorCollection<DeeplyNestedPoco>()
                        {
                            new FieldSelector<DeeplyNestedPoco, DeeplyNestedPoco>(x => x),
                            new FieldSelector<DeeplyNestedPoco, IEnumerable<NestedPoco>, int>(
                                x => x.NestedPocos,
                                x => x.Id,
                                "DeeplyNestedPocoId"
                            )
                        });
                    //TODO: this sucks, we have to enumerate twice; need to fix
                    repo.Save(
                        deeplyNested.SelectMany(x => x.NestedPocos),
                        new FieldSelectorCollection<NestedPoco>()
                        {
                            new FieldSelector<NestedPoco, IEnumerable<Poco>, int>(
                                x => x.Pocos,
                                x => x.Id,
                                "NestedPocoId"
                            )
                        });
                    break;

                case Run.DeeplyNestedExtended:
                    var deeplyNestedExtended = DataFactory.DeeplyNestedPocos(100).ToList();
                    if (writeToConsole)
                    {
                        Console.WriteLine("##########DEEPLYNESTED##########");
                        Console.WriteLine(JsonConvert.SerializeObject(deeplyNestedExtended, Formatting.Indented));
                    }
                    repo.Save(
                        deeplyNestedExtended.AsEnumerable(),
                        new FieldSelectorCollection<DeeplyNestedPoco>()
                        {
                            new FieldSelector<DeeplyNestedPoco, DeeplyNestedPoco>(x => x),
                            new FieldSelector<DeeplyNestedPoco, IEnumerable<NestedPoco>, int>(
                                x => x.NestedPocos,
                                x => x.Id,
                                "DeeplyNestedPocoId"
                            ),
                           new FieldSelector<DeeplyNestedPoco,  IEnumerable<Poco>, NestedPoco,int>(
                               x => x.NestedPocos
                                   .Select(n => new ForeignKeyModelPair<NestedPoco, IEnumerable<Poco>>(n,  n.Pocos)),
                               x => x.Id
                            )
                        });
                    break;

                case Run.Complex:
                    var mapper = InitMapper();
                    var complex = DataFactory.ComplexPocos(1000);
                    if(writeToConsole)
                    {
                        Console.WriteLine("##########COMPLEX##########");
                        Console.WriteLine(JsonConvert.SerializeObject(complex, Formatting.Indented));
                    }
                    repo.Save(
                        complex,
                        new FieldSelectorCollection<ComplexPoco>()
                        {
                            new FieldSelector<ComplexPoco, Poco, int>(
                                x => x.InnerPoco,
                                x => x.Id,
                                "ComplexPocoId"
                            ),
                            new FieldSelector<ComplexPoco, IDictionary<string, string>, int>(
                                x => x.SimpleDictionary,
                                x => x.Id,
                                "ComplexPocoId"
                            ),
                            new FieldSelector<ComplexPoco, IEnumerable<DictPoco>, int>(
                                x => x.ComplexDictionary.Select(
                                    p => mapper.Map(p.Value, new DictPoco{ DictKey = p.Key })
                                ),
                                x => x.Id,
                                "ComplexPocoId"
                            ),
                            //TODO: add possibilty to map enumerables of 'single value' objects without mapping
                            new FieldSelector<ComplexPoco, IEnumerable<SingleValue<string>>, int>(
                                x => x.SimpleCollection.Select(p => new SingleValue<string>(p)),
                                x => x.Id,
                                "ComplexPocoId"
                            ),
                            new FieldSelector<ComplexPoco, IEnumerable<Poco>, int>(
                                x => x.ComplexCollection,
                                x => x.Id,
                                "ComplexPocoId"
                            )
                        }
                    );
                    break;
            }
            stopWatch.Stop();
            
            Console.WriteLine($"All done in {stopWatch.ElapsedMilliseconds}ms. Press any key.");
            Console.ReadKey(true);

            //TODO: add some examples on how to set up dependency injection
        }

        public static IMapper InitMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Poco, DictPoco>();
            });
            return config.CreateMapper();
        }

        private enum Run
        {
            Simple, Nested, DeeplyNested, DeeplyNestedExtended, Complex, SimpleNamedCollections, NestedNamedCollections
        }

    }
}
