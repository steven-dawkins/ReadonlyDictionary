namespace ReadonlyDictionary.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using ProtoBuf;

    public class JsonFlyweightSerializer<T> : ISerializer<T>
    {
        private readonly JsonSerializerSettings settings;
        private readonly FlyweightDataContractResolver contract;
        private readonly JsonFlyweightConverter<string> converter;

        public JsonFlyweightSerializer()
            : this(new FlyweightDataContractResolver(), new JsonFlyweightConverter<string>())
        {

        }

        public JsonFlyweightSerializer(object state)
            : this((JsonFlyweightSerializerState)state)
        {

        }

        public JsonFlyweightSerializer(JsonFlyweightSerializerState state)
            : this(
                  new FlyweightDataContractResolver(state.Contract),
                  new JsonFlyweightConverter<string>(state.Converter))
        {
        }

        private JsonFlyweightSerializer(
            FlyweightDataContractResolver contract,
            JsonFlyweightConverter<string> converter)
        {
            this.converter = converter;
            this.contract = contract;
            this.settings = new JsonSerializerSettings()
            {
                Converters = new [] { this.converter },
                ContractResolver = contract
            };
        }

        public class JsonFlyweightSerializerState
        {
            public readonly FlyweightDataContractResolver.FlyweightDataContractResolverState Contract;
            public readonly JsonFlyweightConverter<string>.JsonFlyweightConverterState Converter;

            public JsonFlyweightSerializerState(
                FlyweightDataContractResolver.FlyweightDataContractResolverState contract,
                JsonFlyweightConverter<string>.JsonFlyweightConverterState converter)
            {
                this.Contract = contract;
                this.Converter = converter;
            }
        }


        public JsonFlyweightSerializerState Serialize()
        {
            return new JsonFlyweightSerializerState(this.contract.Serialize(), this.converter.Serialize());
        }

        public object GetState()
        {
            return this.Serialize();
        }

        public byte[] Serialize(T value)
        {
            var json = JsonConvert.SerializeObject(value, this.settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, this.settings);
        }

        // borrowed from stackoverflow: http://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c/10966684
        public class Map<T1, T2>
        {
            private readonly Dictionary<T1, T2> forward;
            private readonly Dictionary<T2, T1> reverse;

            public Map() : this(new Dictionary<T1, T2>(), new Dictionary<T2, T1>())
            {

            }

            public Map(MapState state) : this(state.Forward, state.Reverse)
            {
            }

            private Map(Dictionary<T1, T2> forward, Dictionary<T2, T1> reverse)
            {
                this.forward = forward;
                this.reverse = reverse;

                this.Forward = new Indexer<T1, T2>(forward);
                this.Reverse = new Indexer<T2, T1>(reverse);
            }

            public class MapState
            {
                public readonly Dictionary<T1, T2> Forward;
                public readonly Dictionary<T2, T1> Reverse;

                public MapState(Dictionary<T1, T2> forward, Dictionary<T2, T1> reverse)
                {
                    this.Forward = forward;
                    this.Reverse = reverse;
                }

            }

            public MapState Serialize()
            {
                return new MapState(
                    forward: this.forward,
                    reverse: this.reverse
                );
            }


            public class Indexer<T3, T4>
            {
                private Dictionary<T3, T4> dictionary;

                public Indexer(Dictionary<T3, T4> dictionary)
                {
                    this.dictionary = dictionary;
                }

                public T4 this[T3 index]
                {
                    get { return this.dictionary[index]; }
                    set { this.dictionary[index] = value; }
                }

                internal bool ContainsKey(T3 key)
                {
                    return this.dictionary.ContainsKey(key);
                }
            }

            public int Count
            {
                get
                {
                    return this.forward.Count;
                }
            }

            public void Add(T1 t1, T2 t2)
            {
                this.forward.Add(t1, t2);
                this.reverse.Add(t2, t1);
            }

            public Indexer<T1, T2> Forward { get; private set; }
            public Indexer<T2, T1> Reverse { get; private set; }
        }

        public class FlyweightDataContractResolver : DefaultContractResolver
        {
            public static readonly FlyweightDataContractResolver Instance = new FlyweightDataContractResolver();

            private readonly Map<string, int> dictionary;

            public FlyweightDataContractResolver()
                : this(new Map<string, int>())
            {
            }

            public FlyweightDataContractResolver(FlyweightDataContractResolverState state)
                : this(state.Dictionary)
            {
            }

            private FlyweightDataContractResolver(Map<string, int> dictionary)
            {
                this.dictionary = dictionary;
            }

            public FlyweightDataContractResolverState Serialize()
            {
                return new FlyweightDataContractResolverState(this.dictionary);
            }

            public class FlyweightDataContractResolverState
            {
                public readonly Map<string, int> Dictionary;

                public FlyweightDataContractResolverState(Map<string, int> dictionary)
                {
                    this.Dictionary = dictionary;
                }
            }


            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (!this.dictionary.Forward.ContainsKey(property.PropertyName))
                {
                    this.dictionary.Add(property.PropertyName, this.dictionary.Count + 1);
                }

                property.PropertyName = this.dictionary.Forward[property.PropertyName].ToString();

                return property;
            }

            protected override string ResolvePropertyName(string propertyName)
            {
                int temp;

                if (int.TryParse(propertyName, out temp))
                {
                    return this.dictionary.Reverse[temp];
                }
                else
                {
                    return base.ResolvePropertyName(propertyName);
                }
            }
        }

        public class JsonFlyweightConverter<ConvertType> : JsonConverter
        {
            private readonly Map<string, int> dictionary;

            public JsonFlyweightConverter()
                : this(new Map<string, int>())
            {

            }

            public JsonFlyweightConverter(JsonFlyweightConverterState state)
            {
                this.dictionary = new Map<string, int>(state.Dictionary);
            }

            private JsonFlyweightConverter(Map<string, int> dictionary)
            {
                this.dictionary = dictionary;
            }

            public class JsonFlyweightConverterState
            {
                public readonly Map<string, int>.MapState Dictionary;

                public JsonFlyweightConverterState(Map<string, int>.MapState dictionary)
                {
                    this.Dictionary = dictionary;
                }
            }


            public JsonFlyweightConverterState Serialize()
            {
                return new JsonFlyweightConverterState(
                    dictionary: this.dictionary.Serialize()
                );

            }

            protected ConvertType Create(Type objectType, JObject jObject)
            {
                return default(ConvertType);
            }

            public override bool CanConvert(Type objectType)
            {
                var result = typeof(ConvertType).IsAssignableFrom(objectType);

                return result;
            }

            public override object ReadJson(JsonReader reader,
                                            Type objectType,
                                            object existingValue,
                                            JsonSerializer serializer)
            {
                var index = (long)reader.Value;

                return this.dictionary.Reverse[(int)index];
            }

            public override void WriteJson(JsonWriter writer,
                                           object value,
                                           JsonSerializer serializer)
            {
                if (!this.dictionary.Forward.ContainsKey((string)value))
                {
                    this.dictionary.Add((string)value, this.dictionary.Count + 1);
                }

                writer.WriteValue(this.dictionary.Forward[(string)value]);
            }
        }
    }


}
