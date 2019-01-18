namespace ReadonlyDictionaryTests.SampleData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public struct Book
    {
        [ProtoMember(1)]
        public string Name { get; private set; }

        [ProtoMember(2)]
        public string Name2 { get; private set; }

        [ProtoMember(3)]
        public string Name3 { get; private set; }

        [JsonConstructor]
        public Book(string name, string name2, string name3)
            : this()
        {

            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Empty name", "name");
            }

            this.Name = name;
            this.Name2 = name2;
            this.Name3 = name3;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Book)
            {
                var b2 = (Book)obj;
                return this.Name == b2.Name;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return String.Format("Book({0})", this.Name);
        }
    }
}
