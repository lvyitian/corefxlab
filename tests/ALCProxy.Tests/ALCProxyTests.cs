// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using ALCProxy.Proxy;
using Xunit;

namespace ALCProxy.Tests
{
    public interface ITest
    {
        public string PrintContext();
        public int DoThing2(int a, List<string> list);
        public int DoThing3(int a, Test2 t);
    }

    public interface IGeneric<T>
    {
        public string PrintContext();
        public int DoThing2(int a, List<string> list);
        public int DoThing3(int a, Test2 t);
        public string DoThing4(T t);
    }
    public class Test2
    {
        public int test;

        public Test2()
        {
            test = 5;
        }
        public Test2(int start)
        {
            test = start;
        }

        public void DoThingy()
        {
            test++;
        }
    }
    public class GenericClass<T> : IGeneric<T>
    {
        string instance = "testString";
        private T instance2;

        public GenericClass()
        {
        }
        public GenericClass(T t)
        {
            instance2 = t;
        }

        public string PrintContext()
        {
            return instance.ToString();
        }
        public int DoThing2(int a, List<string> list)
        {
            return instance.Length;
        }

        public int DoThing3(int a, Test2 t)
        {
            t.DoThingy();
            return 6;
        }
        public string DoThing4(T tester)
        {
            return tester.ToString();
        }
    }
    public class Test : ITest
    {
        public string PrintContext()
        {
            var a = Assembly.GetExecutingAssembly();
            return AssemblyLoadContext.GetLoadContext(a).Name;
        }
        public int DoThing2(int a, List<string> list)
        {
            Console.WriteLine(a);

            return a + list[0].Length;
        }
        public int DoThing3(int a, Test2 t)
        {
            t.DoThingy();
            return 5;
        }
    }
    public class ALCProxyTests
    {
        [Fact]
        public void TestBasicContextLoading()
        {
            SetDirectory();
            AssemblyLoadContext alc = new AssemblyLoadContext("TestContext", isCollectible: true);
            ITest t = ProxyBuilder<ITest>.CreateInstanceAndUnwrap(alc, Assembly.GetExecutingAssembly().CodeBase.Substring(8), "Test");

            //Test basic functionality
            Assert.Equal("TestContext", t.PrintContext());
            Assert.Equal(17, t.DoThing2(10, new List<string> { "Letters", "test", "hello world!"}));

            //Tests for call-by-value functionality, to make sure there aren't any problems with editing objects in the other ALC
            Test2 t2 = new Test2(3);
            Assert.Equal(3, t2.test);
            Assert.Equal(5, t.DoThing3(5, t2));
            Assert.Equal(3, t2.test);

        }

        [Fact]
        public void TestUnload()
        {
            SetDirectory();
            //TODO change to CWT?
            var cwt = new ConditionalWeakTable<string, AssemblyLoadContext>();
            AssemblyLoadContext alc = new AssemblyLoadContext("TestContext2", isCollectible: true);
            cwt.Add("Test", alc);
            ITest t = ProxyBuilder<ITest>.CreateInstanceAndUnwrap(alc, Assembly.GetExecutingAssembly().CodeBase.Substring(8), "Test"); //The one referenced through the comm object, to test that the reference is removed
            Assert.Equal("TestContext2", t.PrintContext());

            alc.Unload();
            alc = null;
            GC.Collect();
            GC.Collect();
            Assert.ThrowsAny<Exception>(t.PrintContext);
            Assert.True(cwt.TryGetValue("Test", out alc));
            Assert.ThrowsAny<Exception>(() => alc.Unload()); //TODO get debugger fixed so I can look at what's going on here more closely
        }


        [Fact]
        public void TestSimpleGenerics()
        {
            SetDirectory();
            AssemblyLoadContext alc = new AssemblyLoadContext("TestContext3", isCollectible: true);
            IGeneric<string> t = ProxyBuilder<IGeneric<string>>.CreateGenericInstanceAndUnwrap(alc, Assembly.GetExecutingAssembly().CodeBase.Substring(8), "GenericClass", new Type[] { typeof(string) }); //The one referenced through the comm object, to test that the reference is removed

            Assert.Equal("testString", t.PrintContext());
            Assert.Equal("Hello!", t.DoThing4("Hello!"));

        }

        [Fact]
        public void TestUserGenerics()
        {
            SetDirectory();
            AssemblyLoadContext alc = new AssemblyLoadContext("TestContext3", isCollectible: true);
            IGeneric<Test2> t = ProxyBuilder<IGeneric<Test2>>.CreateGenericInstanceAndUnwrap(alc, Assembly.GetExecutingAssembly().CodeBase.Substring(8), "GenericClass", new Type[] { typeof(Test2) }); //The one referenced through the comm object, to test that the reference is removed

            Assert.Equal(new Test2().ToString(), t.DoThing4(new Test2()));
        }

        private void SetDirectory()
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
