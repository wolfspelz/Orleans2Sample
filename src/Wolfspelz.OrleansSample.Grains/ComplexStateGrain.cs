using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Wolfspelz.OrleansSample.GrainInterfaces;

namespace Wolfspelz.OrleansSample.Grains
{
    public class ClassA
    {
        public int IntMem;
    }

    public class ComplexStateGrainState
    {
        public int IntProp { get; set; }
        public long LongProp { get; set; }
        public string StringProp { get; set; }
        public double DoubleProp { get; set; }
        public DateTime DateTimeProp { get; set; }

        public int IntMem;
        public long LongMem;
        public string StringMem;
        public double DoubleMem;
        public DateTime DateTimeMem;

        public List<string> ListStringProp;
        public Dictionary<string, string> DictionaryStringStringProp;
        public Dictionary<string, List<string>> DictionaryStringListStringProp;

        public IStringCache GrainProp;
        public ClassA ReferenceProp;
        public List<ClassA> ListReferenceProp;
    }

    public class ComplexStateGrain : Grain<ComplexStateGrainState>, IComplexState
    {
        public class Default
        {
            public static int Int = 42;
            public static long Long = 42000000000;
            public static string String = "Hello World";
            public static double Double = 3.1415926;
            public static DateTime DateTime = new DateTime(1969, 07, 21, 13, 32, 00, DateTimeKind.Utc);

            public static readonly List<string> ListString = new List<string> { "a", "b" };
            public static readonly Dictionary<string, string> DictionaryStringString = new Dictionary<string, string> { ["a"] = "b", ["c"] = "d", };
            public static readonly Dictionary<string, List<string>> DictionaryStringListString = new Dictionary<string, List<string>> { ["a"] = new List<string> { "b", "c" }, ["d"] = new List<string> { "e", "f" }, };

            public static string GrainPropValue = "Tranquility";
            public static readonly ClassA ReferenceProp = new ClassA() { IntMem = 42 };
            public static readonly List<ClassA> ListReferenceProp = new List<ClassA> { new ClassA() { IntMem = 42 }, new ClassA() { IntMem = 43 } };
        }

        private IStringCache GrainProp { get; set; }

        public async Task SaveState()
        {
            State.IntProp = Default.Int;
            State.LongProp = Default.Long;
            State.StringProp = Default.String;
            State.DoubleProp = Default.Double;
            State.DateTimeProp = Default.DateTime;

            State.IntMem = Default.Int;
            State.LongMem = Default.Long;
            State.StringMem = Default.String;
            State.DoubleMem = Default.Double;
            State.DateTimeMem = Default.DateTime;

            State.ListStringProp = Default.ListString;
            State.DictionaryStringStringProp = Default.DictionaryStringString;
            State.DictionaryStringListStringProp = Default.DictionaryStringListString;

            GrainProp = State.GrainProp = GrainFactory.GetGrain<IStringCache>(Guid.NewGuid().ToString());
            await GrainProp.Set(Default.GrainPropValue);

            State.ReferenceProp = Default.ReferenceProp;
            State.ListReferenceProp = Default.ListReferenceProp;

            await base.WriteStateAsync();
        }

        public async Task<bool> CheckState()
        {
            var ok = true;

            State.IntProp = 0;
            State.LongProp = 0;
            State.StringProp = "";
            State.DoubleProp = 0.0;
            State.DateTimeProp = DateTime.MinValue;

            State.IntMem = 0;
            State.LongMem = 0;
            State.StringMem = "";
            State.DoubleMem = 0.0;
            State.DateTimeMem = DateTime.MinValue;

            State.ListStringProp = null;
            State.DictionaryStringStringProp = null;
            State.DictionaryStringListStringProp = null;

            State.GrainProp = null;

            await base.ReadStateAsync();

            ok &= State.IntProp == Default.Int;
            ok &= State.LongProp == Default.Long;
            ok &= State.StringProp == Default.String;
            ok &= Math.Abs(State.DoubleProp - Default.Double) < 0.000001;
            ok &= State.DateTimeProp == Default.DateTime;

            ok &= State.IntMem == Default.Int;
            ok &= State.LongMem == Default.Long;
            ok &= State.StringMem == Default.String;
            ok &= Math.Abs(State.DoubleMem - Default.Double) < 0.000001;
            ok &= State.DateTimeMem == Default.DateTime;

            ok &= State.ListStringProp[0] == "a" && State.ListStringProp[1] == "b";
            ok &= State.DictionaryStringStringProp["a"] == "b" && State.DictionaryStringStringProp["c"] == "d";
            ok &= State.DictionaryStringListStringProp["a"][0] == "b" && State.DictionaryStringListStringProp["a"][1] == "c" && State.DictionaryStringListStringProp["d"][0] == "e" && State.DictionaryStringListStringProp["d"][1] == "f";

            ok &= await GrainProp.Get() == Default.GrainPropValue;

            ok &= State.ReferenceProp.IntMem == Default.ReferenceProp.IntMem;
            ok &= State.ListReferenceProp[0].IntMem == Default.ListReferenceProp[0].IntMem && State.ListReferenceProp[1].IntMem == Default.ListReferenceProp[1].IntMem;

            return ok;
        }
    }
}